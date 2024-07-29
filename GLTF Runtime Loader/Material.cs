using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// A material defined in a GLTF file.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(Name)},nq}}")]
    public class Material
    {
        /// <summary>
        /// Gets the name of this material.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A set of parameter values that are used to define the metallic-roughness material model from Physically Based Rendering (PBR) methodology. When undefined, all the default values of pbrMetallicRoughness MUST apply.
        /// </summary>
        public PBRMetallicRoughness PBRMetallicRoughness { get; }

        /// <summary>
        /// The tangent space normal texture.
        /// </summary>
        public NormalTextureInfo? NormalTexture { get; }

        /// <summary>
        /// The occlusion texture.
        /// </summary>
        public OcclusionTextureInfo? OcclusionTexture { get; }

        /// <summary>
        /// The emissive texture.
        /// </summary>
        public TextureInfo? EmissiveTexture { get; }

        /// <summary>
        /// The factors for the emissive color of the material.
        /// </summary>
        public XYZ EmissiveFactor { get; }

        /// <summary>
        /// The alpha rendering mode of the material.
        /// </summary>
        public string AlphaMode { get; }

        /// <summary>
        /// The alpha cutoff value of the material.
        /// </summary>
        public float AlphaCutoff { get; }

        /// <summary>
        /// Specifies whether the material is double sided.
        /// </summary>
        public bool DoubleSided { get; }

        internal Material(JsonNode source, ReadOnlyCollection<Texture> textures)
        {
            Name = source["name"]!.GetValue<string>();

            var mrNode = source["pbrMetallicRoughness"];
            if (mrNode == null)
                PBRMetallicRoughness = new PBRMetallicRoughness();
            else
                PBRMetallicRoughness = new PBRMetallicRoughness(mrNode, textures);

            var normalNode = source["normalTexture"];
            if (normalNode != null)
                NormalTexture = new NormalTextureInfo(normalNode, textures);

            var occlusionNode = source["occlusionTexture"];
            if (occlusionNode != null)
                OcclusionTexture = new OcclusionTextureInfo(occlusionNode, textures);

            var emissiveNode = source["emissiveTexture"];
            if (emissiveNode != null)
                EmissiveTexture = new TextureInfo(emissiveNode, textures);

            AlphaMode = source["alphaMode"]?.GetValue<string>() ?? "OPAQUE";
            AlphaCutoff = source["alphaCutoff"]?.GetValue<float>() ?? 0.5f;
            DoubleSided = source["doubleSided"]?.GetValue<bool>() ?? false;
        }
    }
}