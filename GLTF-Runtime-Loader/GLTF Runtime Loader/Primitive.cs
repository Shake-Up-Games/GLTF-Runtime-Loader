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
        /// </summary>
        internal static ReadOnlyCollection<object> CastAsPrimitiveData(PrimitiveAttributes key, ReadOnlyCollection<ReadOnlyCollection<object>> data)
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
            return key switch
            {
                PrimitiveAttributes.POSITION or PrimitiveAttributes.NORMAL or PrimitiveAttributes.TANGENT => castAsArrays<float>(data),
                PrimitiveAttributes.TEXCOORD => castAsArrays<float>(data),
                PrimitiveAttributes.JOINTS => castAsArrays<byte>(data),
                PrimitiveAttributes.WEIGHTS => castAsArrays<float>(data),
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