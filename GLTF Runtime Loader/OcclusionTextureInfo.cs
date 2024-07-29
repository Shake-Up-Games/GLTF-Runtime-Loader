using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Reference to a texture.
    /// </summary>
    public class OcclusionTextureInfo : TextureInfo
    {
        /// <summary>
        /// A scalar multiplier controlling the amount of occlusion applied.
        /// </summary>
        public float Strength { get; }
        internal OcclusionTextureInfo(JsonNode source, ReadOnlyCollection<Texture> textures)
            : base(source, textures)
        {
            Strength = source["strength"]?.GetValue<float>() ?? 1.0f;
        }
    }
}