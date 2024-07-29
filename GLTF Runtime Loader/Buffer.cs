using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Represents a buffer of bytes declared or described in a GLTF file.
    /// </summary>
    public sealed class Buffer
    {
        /// <summary>
        /// Gets the URI that defines this buffer, or null if it was declared directly in the JSON file.
        /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
        // This is a GLTF Uri, not anything the System.Uri class is needed for.
        public string? URI { get; }
#pragma warning restore CA1056 // URI-like properties should not be strings

        /// <summary>
        /// Gets an array of the bytes in this buffer.
        /// <para>Do not write to this.</para>
        /// </summary>
        internal byte[] Bytes { get; }

        internal Buffer(JsonNode bufferNode, string binDirectory)
        {
            URI = bufferNode["uri"]?.GetValue<string>();
            if (URI == null)
                throw new NotImplementedException("Non-binary buffers are not yet implemented.");

            Bytes = File.ReadAllBytes(Path.Combine(binDirectory, URI));
        }
    }
}
