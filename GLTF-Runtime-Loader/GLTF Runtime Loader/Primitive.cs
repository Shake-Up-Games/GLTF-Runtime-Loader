using ShakeUpGames.Common3D;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace GLTFRuntime
{

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
        /// Boxed as whichever integer type <see cref="IndexType"/> reports - glTF allows index accessors to be
        /// unsigned byte, unsigned short, or unsigned int (never a signed type or float), so a single fixed
        /// element type isn't wide enough for every legal mesh (e.g. more than 65535 vertices needs uint indices).
        /// </summary>
        public ReadOnlyCollection<object>? Indices { get; }

        /// <summary>
        /// Gets the runtime type of the integers boxed in <see cref="Indices"/> (<see cref="byte"/>,
        /// <see cref="ushort"/>, or <see cref="uint"/>), or null if <see cref="Indices"/> is null or empty.
        /// </summary>
        public Type? IndexType => Indices?.FirstOrDefault()?.GetType();

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

        /// <summary>
        /// The only <see cref="AccessorComponentType"/> values the glTF 2.0 specification allows for an index
        /// accessor - unsigned byte, unsigned short, or unsigned int. Signed integer types and float are not legal
        /// here even though they're legal <see cref="AccessorComponentType"/> values for other accessor uses.
        /// </summary>
        static readonly AccessorComponentType[] indexAccessorTypes =
            [AccessorComponentType.Byte, AccessorComponentType.UShort, AccessorComponentType.UInt];

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
                if (indexAccessor.Type != AccessorDataType.SCALAR || !indexAccessorTypes.Contains(indexAccessor.ComponentType))
                    throw new InvalidOperationException("The index accessor does not define a scalar unsigned byte, unsigned short, or unsigned int array.");

                Indices = new ReadOnlyCollection<object>((from vector in indexAccessor.Data select vector[0]).ToList());
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
}