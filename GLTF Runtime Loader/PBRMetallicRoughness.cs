using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// A set of parameter values that are used to define the metallic-roughness material model from Physically-Based Rendering(PBR) methodology.
    /// </summary>
    public class PBRMetallicRoughness
    {
        /// <summary>
        /// The factors for the base color of the material.
        /// </summary>
        public float[] BaseColorFactor { get; }

        /// <summary>
        /// The base color texture.
        /// </summary>
        public TextureInfo? BaseColorTexture { get; }

        /// <summary>
        /// The factor for the metalness of the material.
        /// </summary>
        public float MetallicFactor { get; }

        /// <summary>
        /// The factor for the roughness of the material.
        /// </summary>
        public float RoughnessFactor { get; }

        /// <summary>
        /// The metallic-roughness texture.
        /// </summary>
        public TextureInfo? MetallicRoughnessTexture { get; }

        internal PBRMetallicRoughness(JsonNode source, ReadOnlyCollection<Texture> textures)
        {
            var colorNode = source["baseColorFactor"];
            if (colorNode == null)
                BaseColorFactor = [0f, 0f, 0f, 0f];
            else
                BaseColorFactor = GLTFHelpers.GetDataFromArray<float>(colorNode.AsArray());

            var textureNode = source["baseColorTexture"];
            if (textureNode != null)
                BaseColorTexture = new TextureInfo(textureNode, textures);

            MetallicFactor = source["metallicFactor"]?.GetValue<float>() ?? 1.0f;
            RoughnessFactor = source["roughnessFactor"]?.GetValue<float>() ?? 1.0f;

            var mrtNode = source["metallicRoughtnessTexture"];
            if (mrtNode != null)
                MetallicRoughnessTexture = new TextureInfo(mrtNode, textures);
        }

        internal PBRMetallicRoughness()
            : this(JsonNode.Parse("{}")!, ReadOnlyCollection<Texture>.Empty)
        {
        }
    }
}