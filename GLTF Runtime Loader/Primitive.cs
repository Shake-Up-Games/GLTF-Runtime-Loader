using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace GLTFRuntime
{
    /// <summary>
    /// Describes a vertex attribute for use in creating vertex elements.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
    public readonly struct VertexAttribute
    {
        /// <summary>
        /// The attribute type for this vertex attribute.
        /// </summary>
        public readonly PrimitiveAttributes Type;

        /// <summary>
        /// The usage index for this vertex attribute, or null if none was specified.
        /// <para>For example, if the library specifies TEXCOORD_1, this is 1.</para>
        /// </summary>
        public readonly int? UsageIndex;

        /// <summary>
        /// Constructs a new vertex attribute.
        /// </summary>
        /// <param name="type">The type of primitive this attribute represents.</param>
        /// <param name="usageIndex">An optional usage index.</param>
        internal VertexAttribute(PrimitiveAttributes type, int? usageIndex)
        {
            Type = type;
            UsageIndex = usageIndex;
        }

        /// <summary>
        /// Returns a human-friendly string representation of this attribute.
        /// </summary>
        public override string ToString()
        {
            if (UsageIndex == null)
                return Type.ToString();
            else
                return $"{Type}_{UsageIndex}";
        }
    }

    /// <summary>
    /// Geometry to be rendered with the given material.
    /// </summary>    
    [DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
    public class Primitive
    {
        /// <summary>
        /// A read-only dictionary where each key corresponds to a mesh attribute semantic
        /// and each value is the attribute’s data.
        /// </summary>
        public ReadOnlyDictionary<VertexAttribute, ReadOnlyCollection<object>> Attributes { get; }

        /// <summary>
        /// The vertex indices. When this is null, the primitive defines non-indexed geometry.
        /// </summary>
        public Collection<ushort>? Indices { get; }

        /// <summary>
        /// The material to apply to this primitive when rendering.
        /// </summary>
        public Material? Material { get; }

        /// <summary>
        /// The topology type of primitives to render.
        /// </summary>
        public TopologyMode Mode { get; }

        /// <summary>
        /// A regex matching attributes strings that  end in _ and a number.
        /// </summary>
        static readonly Regex indexEnd = new Regex(@"_\d$");

        internal Primitive(JsonNode source, Accessor[] accessors, ReadOnlyCollection<Material> materials)
        {
            Dictionary<VertexAttribute, ReadOnlyCollection<object>> attributes = new Dictionary<VertexAttribute, ReadOnlyCollection<object>>();
            var attributesNode = source["attributes"]!.AsObject();
            foreach (var attributeName in attributesNode)
            {
                string key = attributeName.Key;
                int? usageIndex = null;
                string keyTypeString;
                var index = indexEnd.Match(key);
                if (index.Success)
                {
                    keyTypeString = key.Substring(0, index.Index);
                    usageIndex = int.Parse(key.Substring(index.Index + 1));
                }
                else
                    keyTypeString = key;
                var keyType = Enum.Parse<PrimitiveAttributes>(keyTypeString);
                var data = accessors[attributeName.Value!.GetValue<int>()].Data;
                attributes.Add(new VertexAttribute(keyType, usageIndex), CastAsPrimitiveData(keyType, data));
            }

            Attributes = new ReadOnlyDictionary<VertexAttribute, ReadOnlyCollection<object>>(attributes);

            int? indicesIndex = source["indices"]?.GetValue<int>();
            if (indicesIndex != null)
            {
                Accessor indexAccessor = accessors[indicesIndex.Value];
                if (!(indexAccessor.Type == AccessorDataType.SCALAR && indexAccessor.ComponentType == AccessorComponentType.UShort))
                    throw new InvalidOperationException("The index accessor does not define a scalar ushort array.");

                Indices = new Collection<ushort>((from vector in indexAccessor.Data select (ushort)vector[0]).ToList());
            }

            var materialIndex = source["material"]?.GetValue<int>();
            if (materialIndex.HasValue)
                Material = materials[materialIndex.Value];

            Mode = (TopologyMode)(source["mode"]?.GetValue<int>() ?? (int)TopologyMode.TRIANGLES);

            // TODO: Implement morph targets with the targets property here. Finding a gltf model with morph targets would help.
        }

        /// <summary>
        /// Casts a collection of collections of objects into a collection of arrays of specific types of objects to match a type of primitive.
        /// </summary>
        private static ReadOnlyCollection<object> CastAsPrimitiveData(PrimitiveAttributes key, ReadOnlyCollection<ReadOnlyCollection<object>> data)
        {
            ReadOnlyCollection<object> castAsArrays<T>(ReadOnlyCollection<ReadOnlyCollection<object>> data, int arraySize)
            {
                List<object> casted = new List<object>();
                foreach (var vector in data)
                {
                    T[] r = new T[arraySize];
                    Type t = vector[0].GetType();
                    for (int i = 0; i < arraySize; i++)
                    {
                        r[i] = (T)vector[i];
                    }
                    casted.Add(r);
                }
                return new ReadOnlyCollection<object>(casted);
            };
            return key switch
            {
                PrimitiveAttributes.POSITION or PrimitiveAttributes.NORMAL => castAsArrays<float>(data, 3),
                PrimitiveAttributes.TEXCOORD => castAsArrays<float>(data, 2),
                PrimitiveAttributes.JOINTS => castAsArrays<byte>(data, 4),
                PrimitiveAttributes.WEIGHTS => castAsArrays<float>(data, 4),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// Returns a human-friendly string summarizing this primitive.
        /// </summary>
        public override string ToString()
        {
            if (Material == null)
                return $"{Attributes.Values.First().Count} of ({string.Join(", ", Attributes.Keys)})";
            else
                return $"{Attributes.Values.First().Count} of ({string.Join(", ", Attributes.Keys)}) as {Material.Name}";
        }
    }

    /// <summary>
    /// An enumeration of some of the attributes that may appear in <see cref="Primitive.Attributes"/>.
    /// </summary>
    public enum PrimitiveAttributes
    {
        /// <summary>
        /// The position attribute.
        /// </summary>
        POSITION,
        /// <summary>
        /// The normal attribute.
        /// </summary>
        NORMAL,
        /// <summary>
        /// A texture coordinate attribute.
        /// </summary>
        TEXCOORD,
        /// <summary>
        /// A joint indices attribute.
        /// </summary>
        JOINTS,
        /// <summary>
        /// A joint weights attribute.
        /// </summary>
        WEIGHTS
    }
}