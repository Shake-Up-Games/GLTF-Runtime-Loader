using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Image data used to create a texture. Image MAY be referenced by an URI(or IRI) or a buffer view index.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public class Image
    {
        /// <summary>
        /// The URI (or IRI) of the image.
        /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
        public string? URI { get; }
#pragma warning restore CA1056 // URI-like properties should not be strings

        /// <summary>
        /// The image’s media type, if it was defined by a bufferView instead of a URI, or by the media type of a <c>data:</c> URI
        /// (null for a file URI, and for a <c>data:</c> URI that has no media type).
        /// </summary>
        public string? MIMEType { get; }

        /// <summary>
        /// The data from a bufferView or uri that contains the image.
        /// </summary>
        public ReadOnlyCollection<byte>? Data { get; }

        /// <summary>
        /// The user-defined name of this object.
        /// </summary>
        public string? Name { get; }

        internal Image(JsonNode source, BufferView[] bufferViews, string binPath, GltfLoadOptions options)
        {
            Name = source["name"]?.GetValue<string>();

            URI = source["uri"]?.GetValue<string>();
            if (URI == null)
            {
                // Get data from bufferView
                var bufferViewNode = source["bufferView"];
                if (bufferViewNode == null)
                {
                    throwUriBufferViewException();
                    return;
                }

                var bufferView = bufferViews[bufferViewNode.GetValue<int>()];
                Data = new ReadOnlyCollection<byte>(
                    (
                    from array in bufferView.Read(AccessorDataType.SCALAR, AccessorComponentType.Byte, bufferView.ByteLength)
                    select (byte)array[0]
                    ).ToList()
                    );
                MIMEType = source["mimeType"]!.GetValue<string>();
            }
            else if (source["bufferView"] != null)
                throwUriBufferViewException();
            else
            {
                // A file (as given, or combined with the bin path) or a data: URI.
                var (bytes, mediaType) = GltfUri.Read(URI, binPath, options, probeAsGiven: true);
                Data = new ReadOnlyCollection<byte>(bytes);
                MIMEType = mediaType;
            }

            static void throwUriBufferViewException() => throw new InvalidOperationException("An image must have uri or bufferView defined, but not both.");
        }

        private string GetDebuggerDisplay()
        {
            if (URI == null || URI.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return $"{MIMEType} ({Data!.Count} bytes)";
            else
                return URI;
        }
    }
}