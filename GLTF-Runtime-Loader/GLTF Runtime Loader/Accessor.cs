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
        /// The bufferView.
        /// </summary>
        public BufferView BufferView { get; }

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
            BufferView = bufferViews[source["bufferView"]!.GetValue<int>()];
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

            object[][] data = BufferView.Read(Type, ComponentType, Count);
            Data = new ReadOnlyCollection<ReadOnlyCollection<object>>((from inner in data select new ReadOnlyCollection<object>(inner.ToList())).ToList());
        }
    }
}