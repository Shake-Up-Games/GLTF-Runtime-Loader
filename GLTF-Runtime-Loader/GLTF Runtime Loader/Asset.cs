using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Metadata about the glTF asset.
    /// </summary>
    public class Asset
    {
        /// <summary>
        /// A copyright message suitable for display to credit the content creator.
        /// </summary>
        public string? Copyright { get; }

        /// <summary>
        /// Tool that generated this glTF model. Useful for debugging.
        /// </summary>
        public string? Generator { get; }

        /// <summary>
        /// The glTF version in the form of &lt;major&gt;.&lt;minor&gt; that this asset targets.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// The minimum glTF version in the form of &lt;major&gt;.&lt;minor&gt; that this asset targets. This property MUST NOT be greater than the asset version.
        /// </summary>
        public string? MinVersion { get; }


        internal Asset(JsonNode source)
        {
            Copyright = source["copyright"]?.GetValue<string>();
            Generator = source["generator"]?.GetValue<string>();
            Version = source["version"]!.GetValue<string>();
            MinVersion = source["minVersion"]?.GetValue<string>();
        }
    }
}