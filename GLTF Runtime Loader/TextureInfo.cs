using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Reference to a texture.
    /// </summary>
    public class TextureInfo
    {
        /// <summary>
        /// The index of the texture.
        /// </summary>
        public Texture Texture { get; }
                
        /// <summary>
        /// The set index of texture’s TEXCOORD attribute used for texture coordinate mapping.
        /// </summary>
        public int? TexCoord { get; }

        internal TextureInfo(JsonNode source, ReadOnlyCollection<Texture> textures)
        {
            var index = source["index"]!.GetValue<int>();
            Texture = textures[index];

            TexCoord = source["texCoord"]?.GetValue<int>() ?? 0;
        }
    }
}