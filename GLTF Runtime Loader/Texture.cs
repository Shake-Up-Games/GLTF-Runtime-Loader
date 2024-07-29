using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// A texture and its sampler.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public class Texture
    {
        /// <summary>
        /// The sampler used by this texture. When undefined, a sampler with repeat wrapping and auto filtering SHOULD be used.
        /// </summary>
        public TextureSampler? Sampler { get; }

        /// <summary>
        /// The image used by this texture. When undefined, an extension or other mechanism SHOULD supply an alternate texture source, otherwise behavior is undefined.
        /// </summary>
        public Image? Source { get; }

        /// <summary>
        /// The user-defined name of this object.
        /// </summary>
        public string? Name { get; }

        internal Texture(JsonNode source, ReadOnlyCollection<TextureSampler> samplers, ReadOnlyCollection<Image> images)
        {
            JsonNode? samplerNode = source["sampler"];
            if (samplerNode != null)
                Sampler = samplers[samplerNode.GetValue<int>()];

            JsonNode? sourceNode = source["source"];
            if (sourceNode != null)
                Source = images[sourceNode.GetValue<int>()];

            Name = source["name"]?.GetValue<string>();
        }

        private string GetDebuggerDisplay()
        {
            if (Name == null)
            {
                string samplerName = Sampler == null ? "(no sampler)" : (Sampler.Name ?? "(unnamed sampler)");
                string sourceName = Source == null ? "(no sampler)" : (Source.Name ?? "(unnamed sampler)");
                return $"{samplerName} -> {sourceName}";
            }
            else
                return Name;
        }
    }
}