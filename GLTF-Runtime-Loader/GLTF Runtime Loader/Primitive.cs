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
        /// An array of morph targets, or null if this primitive defines none.
        /// <para>Each target is a set of per-vertex displacements (deltas) for POSITION, NORMAL, and/or TANGENT, to be
        /// added to this primitive's base <see cref="Attributes"/> and scaled by a weight to animate the mesh without
        /// skinning (e.g. Blender "shape keys"). See <see cref="MorphTarget"/> for details, including an optional,
        /// non-standard tint-color convention this library defines for use by consumers that need to blend a color
        /// alongside a target's geometry.</para>
        /// <para>The number and order of elements in this array corresponds to the owning <see cref="Mesh"/>'s
        /// <see cref="Mesh.Weights"/> array, when present: the i-th weight applies to the i-th target.</para>
        /// </summary>
        public ReadOnlyCollection<MorphTarget>? Targets { get; }

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
                var accessor = accessors[attributeName.Value!.GetValue<int>()];
                attributes.Add(new VertexAttribute(keyType, usageIndex), CastAsPrimitiveData(keyType, accessor.ComponentType, accessor.Data));
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

            var targetsNode = source["targets"]?.AsArray();
            if (targetsNode != null)
                Targets = new ReadOnlyCollection<MorphTarget>((from targetNode in targetsNode select new MorphTarget(targetNode!, accessors)).ToList());
        }

        /// <summary>
        /// Casts a collection of collections of objects into a collection of arrays of specific types of objects to match a type of primitive.
        /// <para>The array size for each element is taken from the accessor data itself (i.e. from each vector's own length)
        /// rather than being hard-coded per attribute, since some attributes are not a fixed size: for example TANGENT is
        /// a VEC4 (XYZ + handedness) when used as a base primitive attribute, but a VEC3 (XYZ only) when used as a morph
        /// target displacement, per the glTF 2.0 specification.</para>
        /// <para>Per the glTF 2.0 specification, an attribute's legal <paramref name="componentType"/> values depend on
        /// the attribute semantic itself, not just a single hard-coded type: <c>JOINTS_n</c> accessors are legally
        /// <see cref="AccessorComponentType.Byte"/> or <see cref="AccessorComponentType.UShort"/> (literal joint indices,
        /// never normalized), and <c>WEIGHTS_n</c>/<c>TEXCOORD_n</c> accessors are legally <see cref="AccessorComponentType.Float"/>,
        /// or a <i>normalized</i> <see cref="AccessorComponentType.Byte"/>/<see cref="AccessorComponentType.UShort"/> (where a raw
        /// value of <see cref="byte.MaxValue"/>/<see cref="ushort.MaxValue"/> represents <c>1.0</c>). Passing only <paramref name="data"/>
        /// without <paramref name="componentType"/> used to force every attribute through one hard-coded element type, which crashed
        /// with an <see cref="InvalidCastException"/> the moment a file used a different (but equally legal) component type - which is
        /// exactly what a real Blender-exported skinned mesh does for a normalized-unsigned-byte <c>WEIGHTS_0</c> accessor.</para>
        /// </summary>
        /// <param name="key">The attribute semantic being cast, which determines the target CLR element type(s).</param>
        /// <param name="componentType">The source accessor's own <see cref="Accessor.ComponentType"/>, needed because
        /// <paramref name="data"/> alone does not carry this information - each boxed element's runtime type already
        /// matches <paramref name="componentType"/> (see <see cref="BufferView.Read"/>), but only the caller knows what
        /// that type actually is.</param>
        /// <param name="data">The raw accessor data to cast/convert.</param>
        internal static ReadOnlyCollection<object> CastAsPrimitiveData(PrimitiveAttributes key, AccessorComponentType componentType, ReadOnlyCollection<ReadOnlyCollection<object>> data)
        {
            ReadOnlyCollection<object> castAsArrays<T>(ReadOnlyCollection<ReadOnlyCollection<object>> data)
            {
                List<object> casted = new List<object>();
                foreach (var vector in data)
                {
                    T[] r = new T[vector.Count];
                    for (int i = 0; i < vector.Count; i++)
                    {
                        r[i] = (T)vector[i];
                    }
                    casted.Add(r);
                }
                return new ReadOnlyCollection<object>(casted);
            };

            // JOINTS_n is legally an unsigned byte or unsigned short accessor (never normalized - these are literal
            // joint indices). Both are widened to ushort so downstream skinning code has a single consistent type to
            // work with, regardless of which of the two legal source component types a given file actually uses.
            ReadOnlyCollection<object> castJointsAsUShortArrays(ReadOnlyCollection<ReadOnlyCollection<object>> data)
            {
                List<object> casted = new List<object>();
                foreach (var vector in data)
                {
                    ushort[] r = new ushort[vector.Count];
                    for (int i = 0; i < vector.Count; i++)
                    {
                        r[i] = componentType switch
                        {
                            AccessorComponentType.Byte => (byte)vector[i],
                            AccessorComponentType.UShort => (ushort)vector[i],
                            _ => throw new InvalidOperationException($"A JOINTS accessor must use an unsigned byte or unsigned short component type per the glTF 2.0 specification. Found {componentType} instead."),
                        };
                    }
                    casted.Add(r);
                }
                return new ReadOnlyCollection<object>(casted);
            };

            // WEIGHTS_n and TEXCOORD_n are legally float, or a *normalized* unsigned byte/unsigned short, per the
            // glTF 2.0 specification's normalized-integer convention: a raw byte value of 255 (byte.MaxValue) means
            // 1.0, and a raw ushort value of 65535 (ushort.MaxValue) means 1.0. Float data is passed through unchanged.
            ReadOnlyCollection<object> castAsNormalizedFloatArrays(ReadOnlyCollection<ReadOnlyCollection<object>> data)
            {
                List<object> casted = new List<object>();
                foreach (var vector in data)
                {
                    float[] r = new float[vector.Count];
                    for (int i = 0; i < vector.Count; i++)
                    {
                        r[i] = componentType switch
                        {
                            AccessorComponentType.Float => (float)vector[i],
                            AccessorComponentType.Byte => (byte)vector[i] / (float)byte.MaxValue,
                            AccessorComponentType.UShort => (ushort)vector[i] / (float)ushort.MaxValue,
                            _ => throw new InvalidOperationException($"This attribute must use a float, normalized unsigned byte, or normalized unsigned short component type per the glTF 2.0 specification. Found {componentType} instead."),
                        };
                    }
                    casted.Add(r);
                }
                return new ReadOnlyCollection<object>(casted);
            };

            return key switch
            {
                PrimitiveAttributes.POSITION or PrimitiveAttributes.NORMAL or PrimitiveAttributes.TANGENT => castAsArrays<float>(data),
                PrimitiveAttributes.TEXCOORD => castAsNormalizedFloatArrays(data),
                PrimitiveAttributes.JOINTS => castJointsAsUShortArrays(data),
                PrimitiveAttributes.WEIGHTS => castAsNormalizedFloatArrays(data),
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