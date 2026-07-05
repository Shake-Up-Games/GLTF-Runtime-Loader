using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Represents a view into a data buffer.
    /// </summary>
    public class BufferView
    {
        /// <summary>
        /// The name of this buffer view, or null if it has no name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the buffer that this view faces.
        /// </summary>
        public Buffer Buffer { get; }

        /// <summary>
        /// The intended GPU buffer type for this buffer view, or null if none is intended.
        /// </summary>
        public BufferViewTarget? Target { get; }

        /// <summary>
        /// Gets the byte offset indicating where in the buffer this view starts.
        /// </summary>
        public int ByteOffset { get; }

        /// <summary>
        /// Gets the length of this view, in bytes.
        /// </summary>
        public int ByteLength { get; }

        /// <summary>
        /// Constructs a new <see cref="BufferView"/> from a JSON node.
        /// </summary>
        internal BufferView(JsonNode viewNode, Buffer[] buffers)
        {
            if (viewNode["byteStride"] != null)
                throw new NotImplementedException("Loosely packed buffer data is not supported.");

            Name = viewNode["name"]?.GetValue<string>();
            Buffer = buffers[viewNode["buffer"]!.GetValue<int>()];
            ByteLength = viewNode["byteLength"]!.GetValue<int>();
            ByteOffset = viewNode["byteOffset"]!.GetValue<int>();
            var targetNode = viewNode["target"];
            if (targetNode != null)
                Target = (BufferViewTarget)targetNode.GetValue<int>();
        }


        /// <summary>
        /// Reads <paramref name="count"/> elements of the given <paramref name="dataType"/>/<paramref name="componentType"/> out of this buffer view.
        /// </summary>
        /// <param name="dataType">Whether each element is a scalar, vector, or matrix, which determines how many components it has.</param>
        /// <param name="componentType">The datatype of each component, which determines its byte size and how it is decoded.</param>
        /// <param name="count">The number of elements to read.</param>
        /// <param name="byteOffset">An additional byte offset into this buffer view, e.g. an accessor's own byteOffset property, or the byteOffset of a sparse accessor's indices/values sub-object. Defaults to 0.</param>
        internal object[][] Read(AccessorDataType dataType, AccessorComponentType componentType, int count, int byteOffset = 0)
        {
            var span = new Span<byte>(Buffer.Bytes, ByteOffset + byteOffset, ByteLength - byteOffset).ToArray();

            // The dimensions of each inner array
            var componentCount = GLTFHelpers.GetComponentCount(dataType);
            // Attributes for determining the type of reader and the size of the component type
            var ctAttributes = GLTFHelpers.GetComponentTypeInfo(componentType);

            Func<byte[], int, ValueType> reader =
                componentType switch
                {
                    AccessorComponentType.SByte => (bytes, index) => (sbyte)bytes[index],
                    AccessorComponentType.Byte => (bytes, index) => bytes[index],
                    AccessorComponentType.Short => (bytes, index) => BitConverter.ToInt16(bytes, index),
                    AccessorComponentType.UShort => (bytes, index) => BitConverter.ToUInt16(bytes, index),
                    AccessorComponentType.Int => (bytes, index) => BitConverter.ToInt32(bytes, index),
                    AccessorComponentType.UInt => (bytes, index) => BitConverter.ToUInt32(bytes, index),
                    AccessorComponentType.Float => (bytes, index) => BitConverter.ToSingle(bytes, index),
                    _ => throw new NotImplementedException()
                };

            // This is not reading correctly...
            List<object[]> objects = new List<object[]>();
            for (int i = 0; i < count; i++)
            {
                object[] components = new object[componentCount];
                for (int j = 0; j < componentCount; j++)
                {
                    components[j] = reader(span, i * ctAttributes.Size * componentCount + j * ctAttributes.Size);
                }
                objects.Add(components);
            }

            if (objects.Count != count)
                throw new InvalidOperationException("ByteLength does not match the accessor's count.");

            return objects.ToArray();
        }
    }
}