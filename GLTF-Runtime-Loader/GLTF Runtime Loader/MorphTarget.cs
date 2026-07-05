using ShakeUpGames.Common3D;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// A morph target: a set of per-vertex attribute displacements ("deltas") that can be scaled by a weight and added
    /// to a primitive's base vertex data to animate a mesh without skinning (for example, Blender's "shape keys").
    /// <para>Per the glTF 2.0 specification, each entry in <c>primitive.targets</c> is a dictionary whose keys are a
    /// subset of POSITION, NORMAL, and TANGENT (the only attribute semantics the specification permits for morph
    /// targets — TEXCOORD, COLOR, JOINTS, and WEIGHTS are not legal here) and whose values are accessor indices. Each
    /// accessor holds one displacement vector per base vertex: the accessor's element count MUST match the owning
    /// primitive's vertex count, and element <c>i</c> of the target attribute is added (after being scaled by the
    /// corresponding morph weight) to element <c>i</c> of the primitive's base attribute of the same name. TANGENT is a
    /// VEC3 in this context (unlike the VEC4 used for the primitive's base TANGENT attribute), since a target only ever
    /// displaces the tangent direction and never its handedness.</para>
    /// <para>The spec calls out that morph target data is, by nature, sparse — most exporters (including Blender, when
    /// its "Sparse Accessors" export option is used, which is the common/recommended case for shape keys) only store the
    /// vertices that actually move for a given target, via a sparse accessor. This library resolves sparse accessors
    /// transparently: whether a target's attribute accessor is dense or sparse, <see cref="Attributes"/> always exposes
    /// one fully-resolved value per base vertex (with zero displacement for vertices a sparse accessor did not override).</para>
    /// </summary>
    [DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
    public class MorphTarget
    {
        /// <summary>
        /// A regex-free set of the only attribute semantics the glTF 2.0 specification permits on a morph target.
        /// </summary>
        private static readonly PrimitiveAttributes[] legalTargetAttributes = new[]
        {
            PrimitiveAttributes.POSITION,
            PrimitiveAttributes.NORMAL,
            PrimitiveAttributes.TANGENT,
        };

        /// <summary>
        /// A read-only dictionary where each key is one of POSITION, NORMAL, or TANGENT, and each value is this
        /// target's per-vertex displacement data for that attribute (one element per vertex of the owning primitive,
        /// in the same order as the primitive's base <see cref="Primitive.Attributes"/>).
        /// <para>A target is not required to define all three attributes; check <see cref="ReadOnlyDictionary{TKey, TValue}.ContainsKey"/>
        /// before indexing.</para>
        /// </summary>
        public ReadOnlyDictionary<VertexAttribute, ReadOnlyCollection<object>> Attributes { get; }

        /// <summary>
        /// An optional per-target tint color, or null if this target does not define one.
        /// <para><b>This is not part of the core glTF 2.0 specification.</b> Core morph targets only carry geometric
        /// displacements (POSITION/NORMAL/TANGENT) — there is no standard way to associate a color with a target. Since
        /// Shake Up Games projects want to author object state transitions (e.g. "bruised", "wet", "on fire") as a single
        /// morph-target weight that blends both shape <i>and</i> color, this library defines a small, application-specific
        /// convention, stored in the standard glTF <c>extras</c> object (the location the specification itself recommends
        /// for "application-specific data" that any conformant tool must either preserve or silently ignore, with no
        /// extension registration required):</para>
        /// <para><c>primitive.targets[i].extras.SUE_tintColor</c> — a JSON array of 3 (RGB) or 4 (RGBA) numbers in the
        /// [0, 1] range, matching the scale of <see cref="PBRMetallicRoughness.BaseColorFactor"/>. When the array has 3
        /// elements, alpha defaults to 1.0. This property therefore always has 4 elements: [R, G, B, A].</para>
        /// <para>A downstream renderer is expected to blend this color into the base material's color, proportionally to
        /// this target's morph weight, independently of (and in addition to) whatever geometric deltas the same target
        /// carries. A target may legitimately carry a tint with no geometry deltas, geometry deltas with no tint, or both.</para>
        /// <para><b>DCC round-tripping caveat:</b> because this is an invented, non-standard convention rather than a
        /// registered glTF extension, whether it survives a given content pipeline depends entirely on whether that
        /// pipeline's exporter can be made to emit <c>extras</c> on individual target entries. As of this writing, Blender's
        /// stock glTF exporter (io_scene_gltf2) supports exporting Blender custom properties as <c>extras</c> for objects,
        /// meshes, materials, and nodes, but its shape keys (<c>Key.key_blocks</c>) do not appear to expose a built-in
        /// custom-properties/extras hook that the exporter reads per morph target. In practice, authors will likely need
        /// either a small Blender export post-process (e.g. a <c>gltf2_export_user_extensions</c> hook keyed by shape key
        /// name) or a separate script that patches <c>SUE_tintColor</c> into the exported JSON after the fact. This library
        /// only reads the convention once it is present in the file — it does not assert or guarantee that any particular
        /// authoring tool can produce it unassisted.</para>
        /// </summary>
        public float[]? TintColor { get; }

        internal MorphTarget(JsonNode source, Accessor[] accessors)
        {
            Dictionary<VertexAttribute, ReadOnlyCollection<object>> attributes = new Dictionary<VertexAttribute, ReadOnlyCollection<object>>();
            var attributesNode = source.AsObject().Where(kvp => kvp.Key != "extras" && kvp.Key != "extensions");
            foreach (var attribute in attributesNode)
            {
                var keyType = Enum.Parse<PrimitiveAttributes>(attribute.Key);
                if (!legalTargetAttributes.Contains(keyType))
                    throw new InvalidOperationException($"'{attribute.Key}' is not a legal morph target attribute. The glTF 2.0 specification only permits POSITION, NORMAL, and TANGENT on morph targets.");

                var data = accessors[attribute.Value!.GetValue<int>()].Data;
                attributes.Add(new VertexAttribute(keyType, null), Primitive.CastAsPrimitiveData(keyType, data));
            }

            Attributes = new ReadOnlyDictionary<VertexAttribute, ReadOnlyCollection<object>>(attributes);

            var tintNode = source["extras"]?["SUE_tintColor"];
            if (tintNode != null)
            {
                var tintArray = tintNode.AsArray();
                if (tintArray.Count != 3 && tintArray.Count != 4)
                    throw new InvalidOperationException($"extras.SUE_tintColor must have 3 (RGB) or 4 (RGBA) elements, but found {tintArray.Count}.");

                float[] rgba = GLTFHelpers.GetDataFromArray<float>(tintArray);
                TintColor = rgba.Length == 4 ? rgba : new float[] { rgba[0], rgba[1], rgba[2], 1f };
            }
        }

        /// <summary>
        /// Returns a human-friendly string summarizing this morph target.
        /// </summary>
        public override string ToString()
        {
            string attributesText = string.Join(", ", Attributes.Keys);
            if (TintColor == null)
                return attributesText;
            else
                return $"{attributesText}, tint ({string.Join(", ", TintColor)})";
        }
    }
}
