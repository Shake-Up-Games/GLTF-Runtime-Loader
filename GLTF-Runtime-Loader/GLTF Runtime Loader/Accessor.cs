using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// A typed view into a buffer view that contains raw binary data.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(ComponentType)},nq}} {{{nameof(Type)},nq}}[{{{nameof(Count)}}}]")]
    public class Accessor
    {
        /// <summary>
        /// The bufferView, or null if this accessor has no backing storage of its own.
        /// <para>This is only null for a sparse accessor whose <c>bufferView</c> property was omitted, meaning the accessor's
        /// non-overridden elements are implicitly all-zero (see the glTF 2.0 specification's description of sparse accessors).
        /// A non-sparse accessor always has a <see cref="BufferView"/>.</para>
        /// </summary>
        public BufferView? BufferView { get; }

        /// <summary>
        /// The datatype of the accessor’s components.
        /// </summary>
        public AccessorComponentType ComponentType { get; }

        /// <summary>
        /// Specifies whether integer data values are normalized before usage.
        /// </summary>
        public bool Normalized { get; }

        /// <summary>
        /// The number of elements referenced by this accessor.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Maximum value of each component in this accessor.
        /// <para>The type of this property depends on <see cref="ComponentType"/> and <see cref="Type"/>.</para>
        /// </summary>
        public object? Max { get; }
        /// <summary>
        /// Minimum value of each component in this accessor.
        /// <para>The type of this property depends on <see cref="ComponentType"/> and <see cref="Type"/>.</para>
        /// </summary>
        public object? Min { get; }

        /// <summary>
        /// Specifies if the accessor’s elements are scalars, vectors, or matrices.
        /// </summary>
        public AccessorDataType Type { get; }

        /// <summary>
        /// A collection of collections. Each inner collection represents a single value in this accessor, and the outer collection contains them all.
        /// </summary>
        public ReadOnlyCollection<ReadOnlyCollection<object>> Data { get; }

        internal Accessor(JsonNode source, BufferView[] bufferViews)
        {
            int? bufferViewIndex = source["bufferView"]?.GetValue<int>();
            BufferView = bufferViewIndex.HasValue ? bufferViews[bufferViewIndex.Value] : null;
            int ctValue = source["componentType"]!.GetValue<int>();
            if (!Enum.IsDefined(typeof(AccessorComponentType), ctValue))
                throw new NotImplementedException();
            ComponentType = (AccessorComponentType)ctValue;
            Count = source["count"]!.GetValue<int>();
            var minNode = source["min"];
            if (minNode != null)
                Min = GLTFHelpers.GetDataFromArray(minNode.AsArray(), ComponentType);

            var maxNode = source["max"];
            if (maxNode != null)
                Max = GLTFHelpers.GetDataFromArray(maxNode.AsArray(), ComponentType);

            Type = Enum.Parse<AccessorDataType>(source["type"]!.GetValue<string>());

            Normalized = source["normalized"]?.GetValue<bool>() ?? false;

            int byteOffset = source["byteOffset"]?.GetValue<int>() ?? 0;
            object[][] data = BufferView != null
                ? BufferView.Read(Type, ComponentType, Count, byteOffset)
                : CreateZeroFilledData(Type, ComponentType, Count);

            var sparseNode = source["sparse"];
            if (sparseNode != null)
                ApplySparse(sparseNode, bufferViews, data);

            Data = new ReadOnlyCollection<ReadOnlyCollection<object>>((from inner in data select new ReadOnlyCollection<object>(inner.ToList())).ToList());
        }

        /// <summary>
        /// Creates an array of <paramref name="count"/> elements, each holding the zero value appropriate for <paramref name="componentType"/>.
        /// <para>Used for sparse accessors that omit <c>bufferView</c>: per the glTF 2.0 specification, such an accessor's
        /// non-overridden elements are implicitly all-zero.</para>
        /// </summary>
        private static object[][] CreateZeroFilledData(AccessorDataType type, AccessorComponentType componentType, int count)
        {
            int componentCount = GLTFHelpers.GetComponentCount(type);
            object zero = ZeroValue(componentType);
            object[][] data = new object[count][];
            for (int i = 0; i < count; i++)
            {
                object[] element = new object[componentCount];
                for (int j = 0; j < componentCount; j++)
                    element[j] = zero;
                data[i] = element;
            }
            return data;
        }

        /// <summary>
        /// Returns the boxed zero value for the given component type (e.g. <c>0f</c> for <see cref="AccessorComponentType.Float"/>).
        /// </summary>
        private static object ZeroValue(AccessorComponentType componentType) => componentType switch
        {
            AccessorComponentType.SByte => (sbyte)0,
            AccessorComponentType.Byte => (byte)0,
            AccessorComponentType.Short => (short)0,
            AccessorComponentType.UShort => (ushort)0,
            AccessorComponentType.Int => 0,
            AccessorComponentType.UInt => 0u,
            AccessorComponentType.Float => 0f,
            _ => throw new NotImplementedException()
        };

        /// <summary>
        /// Applies a sparse accessor's overrides onto <paramref name="data"/> in place.
        /// <para>Per the glTF 2.0 specification, <c>accessor.sparse</c> stores a small number of "index, value" pairs that
        /// override individual elements of the accessor's data, leaving all other elements as they were (either read from
        /// <c>bufferView</c>, or implicitly zero if <c>bufferView</c> was omitted). This is commonly used for morph targets,
        /// since most exporters (including Blender's) only store the vertices that actually moved.</para>
        /// </summary>
        private void ApplySparse(JsonNode sparseNode, BufferView[] bufferViews, object[][] data)
        {
            int sparseCount = sparseNode["count"]!.GetValue<int>();

            JsonNode indicesNode = sparseNode["indices"]!;
            BufferView indicesBufferView = bufferViews[indicesNode["bufferView"]!.GetValue<int>()];
            int indicesByteOffset = indicesNode["byteOffset"]?.GetValue<int>() ?? 0;
            int indicesComponentTypeValue = indicesNode["componentType"]!.GetValue<int>();
            if (!Enum.IsDefined(typeof(AccessorComponentType), indicesComponentTypeValue))
                throw new NotImplementedException();
            var indicesComponentType = (AccessorComponentType)indicesComponentTypeValue;
            if (indicesComponentType != AccessorComponentType.Byte && indicesComponentType != AccessorComponentType.UShort && indicesComponentType != AccessorComponentType.UInt)
                throw new InvalidOperationException($"A sparse accessor's indices must use an unsigned byte, unsigned short, or unsigned int component type, per the glTF 2.0 specification. Found {indicesComponentType} instead.");

            object[][] rawIndices = indicesBufferView.Read(AccessorDataType.SCALAR, indicesComponentType, sparseCount, indicesByteOffset);

            JsonNode valuesNode = sparseNode["values"]!;
            BufferView valuesBufferView = bufferViews[valuesNode["bufferView"]!.GetValue<int>()];
            int valuesByteOffset = valuesNode["byteOffset"]?.GetValue<int>() ?? 0;
            object[][] rawValues = valuesBufferView.Read(Type, ComponentType, sparseCount, valuesByteOffset);

            for (int i = 0; i < sparseCount; i++)
            {
                int index = Convert.ToInt32(rawIndices[i][0]);
                if (index < 0 || index >= data.Length)
                    throw new InvalidOperationException($"A sparse accessor override targets index {index}, which is outside the accessor's element count of {data.Length}.");
                data[index] = rawValues[i];
            }
        }
    }
}