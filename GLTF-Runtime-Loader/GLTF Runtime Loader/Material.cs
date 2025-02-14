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
        public string Name { get; set; }

        /// <summary>
        /// A set of parameter values that are used to define the metallic-roughness material model from Physically Based Rendering (PBR) methodology. When undefined, all the default values of pbrMetallicRoughness MUST apply.
        /// </summary>
        public PBRMetallicRoughness PBRMetallicRoughness { get; set; }

        /// <summary>
        /// The tangent space normal texture.
        /// </summary>
        public NormalTextureInfo? NormalTexture { get; set; }

        /// <summary>
        /// The occlusion texture.
        /// </summary>
        public OcclusionTextureInfo? OcclusionTexture { get; set; }

        /// <summary>
        /// The emissive texture.
        /// </summary>
        public TextureInfo? EmissiveTexture { get; set; }

        /// <summary>
        /// The factors for the emissive color of the material.
        /// </summary>
        public XYZ EmissiveFactor { get; set; }

        /// <summary>
        /// The alpha rendering mode of the material.
        /// </summary>
        public string AlphaMode { get; set; }

        /// <summary>
        /// The alpha cutoff value of the material.
        /// </summary>
        public float AlphaCutoff { get; set; }

        /// <summary>
        /// Specifies whether the material is double sided.
        /// </summary>
        public bool DoubleSided { get; set; }

        /// <summary>
        /// A parameterless constructor provided for deserialization.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Material() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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