using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Reference to a texture.
    /// </summary>
    public class NormalTextureInfo : TextureInfo
    {
        /// <summary>
        /// The scalar parameter applied to each normal vector of the normal texture.
        /// </summary>
        public float Scale { get; }

        internal NormalTextureInfo(JsonNode source, ReadOnlyCollection<Texture> textures)
            : base(source, textures)
        {
            Scale = source["scale"]?.GetValue<float>() ?? 1.0f;
        }
    }
}