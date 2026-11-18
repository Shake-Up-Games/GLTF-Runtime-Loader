using System.Globalization;
using System.Text;

namespace GLTFRuntime
{
    /// <summary>
    /// The one place where the <c>uri</c> of a glTF buffer or image becomes bytes (glTF 2.0 section 2.8, "URIs"):
    /// <c>data:</c> URIs (RFC 2397) are decoded in memory, and every other uri is a file path that is percent-decoded
    /// according to <see cref="GltfLoadOptions.UriDecoding"/>.
    /// </summary>
    internal static class GltfUri
    {
        private const string DataScheme = "data:";

        /// <summary>
        /// Reads the bytes a uri refers to.
        /// </summary>
        /// <param name="uri">The <c>uri</c> property of a buffer or image.</param>
        /// <param name="directory">The directory that relative paths are resolved against.</param>
        /// <param name="options">How file paths are decoded.</param>
        /// <param name="probeAsGiven">
        /// Whether each candidate path is first tried as given (relative to the process directory, or absolute) before it
        /// is combined with <paramref name="directory"/>. Images have always done this; buffers have not.
        /// </param>
        /// <returns>The bytes, and the media type of a <c>data:</c> URI (null for files and for a data URI without one).</returns>
        internal static (byte[] Bytes, string? MediaType) Read(string uri, string directory, GltfLoadOptions options, bool probeAsGiven)
        {
            if (uri.StartsWith(DataScheme, StringComparison.OrdinalIgnoreCase))
                return ReadDataUri(uri);

            List<string> candidates = new List<string>(2);
            if (options.UriDecoding == GltfUriDecoding.Standard)
            {
                string decoded = Uri.UnescapeDataString(uri);
                candidates.Add(decoded);
                if (!string.Equals(decoded, uri, StringComparison.Ordinal))
                    candidates.Add(uri);
            }
            else
                candidates.Add(uri);

            List<string> tried = new List<string>();
            foreach (string candidate in candidates)
            {
                if (probeAsGiven)
                {
                    if (File.Exists(candidate))
                        return (File.ReadAllBytes(candidate), null);
                    tried.Add(candidate);
                }

                string combined = Path.Combine(directory, candidate);
                if (File.Exists(combined))
                    return (File.ReadAllBytes(combined), null);
                tried.Add(combined);
            }

            throw new FileNotFoundException(
                $"The glTF uri '{uri}' does not match a file. Paths tried: {string.Join("; ", tried)}",
                tried[^1]);
        }

        /// <summary>
        /// Decodes <c>data:[&lt;mediatype&gt;][;base64],&lt;data&gt;</c> as defined by RFC 2397.
        /// </summary>
        private static (byte[] Bytes, string? MediaType) ReadDataUri(string uri)
        {
            int comma = uri.IndexOf(',', StringComparison.Ordinal);
            if (comma < 0)
                throw new InvalidOperationException("A data URI must have a comma between its header and its data (data:[<mediatype>][;base64],<data>).");

            string[] header = uri.Substring(DataScheme.Length, comma - DataScheme.Length).Split(';');
            string? mediaType = header[0].Contains('/', StringComparison.Ordinal) ? header[0] : null;
            bool isBase64 = header.Length > 1 && string.Equals(header[^1], "base64", StringComparison.OrdinalIgnoreCase);
            string payload = uri.Substring(comma + 1);

            if (!isBase64)
                return (PercentDecodeToBytes(payload), mediaType);

            try
            {
                // Some writers percent-encode the padding or the '+' and '/' characters of the base64 text.
                string text = payload.Contains('%', StringComparison.Ordinal) ? Uri.UnescapeDataString(payload) : payload;
                return (Convert.FromBase64String(text), mediaType);
            }
            catch (FormatException ex)
            {
                throw new FormatException("The payload of a base64 data URI is not valid base64 text.", ex);
            }
        }

        /// <summary>
        /// Decodes percent escapes to raw octets. Characters that are not part of a valid escape are kept (as UTF-8).
        /// </summary>
        private static byte[] PercentDecodeToBytes(string text)
        {
            List<byte> bytes = new List<byte>(text.Length);
            int plainStart = 0;
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '%' && TryHex(text, i + 1, out byte value))
                {
                    bytes.AddRange(Encoding.UTF8.GetBytes(text.Substring(plainStart, i - plainStart)));
                    bytes.Add(value);
                    i += 3;
                    plainStart = i;
                }
                else
                    i++;
            }

            bytes.AddRange(Encoding.UTF8.GetBytes(text.Substring(plainStart)));
            return bytes.ToArray();
        }

        private static bool TryHex(string text, int index, out byte value)
        {
            value = 0;
            return index + 1 < text.Length
                && byte.TryParse(text.AsSpan(index, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }
    }
}
