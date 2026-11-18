namespace GLTFRuntime
{
    /// <summary>
    /// How the <c>uri</c> of a buffer or image that names a file is turned into a file path.
    /// </summary>
    /// <remarks>
    /// <para>The glTF 2.0 specification, section 2.8 "URIs", says that relative paths are
    /// "path-noscheme or ipath-noscheme as defined by RFC 3986, Section 4.2 or RFC 3987, Section 2.2 — without scheme,
    /// authority, or parameters. Reserved characters (as defined by RFC 3986, Section 2.2. and RFC 3987, Section 2.2.)
    /// MUST be percent-encoded", and that "Paths with non-ASCII characters MAY be written as-is, with JSON string
    /// escaping, or with percent-encoding; all these options are valid." A file called <c>my texture.png</c> is
    /// therefore written <c>my%20texture.png</c> by a conforming exporter.</para>
    /// <para><c>data:</c> URIs are not affected by this setting: they are always decoded in memory.</para>
    /// </remarks>
    public enum GltfUriDecoding
    {
        /// <summary>
        /// Treat the <c>uri</c> as the specification describes: percent-decode it and look for that file first, then
        /// fall back to the literal, undecoded name. Every file that loaded before this option existed still loads, unless
        /// both the decoded name and the literal name exist and differ, in which case the decoded file is read.
        /// </summary>
        Standard,

        /// <summary>
        /// Use the <c>uri</c> exactly as written, as releases before this option existed did. Percent escapes are not
        /// interpreted, so a <c>uri</c> of <c>my%20texture.png</c> looks only for a file whose name contains
        /// <c>%20</c>.
        /// </summary>
        Literal,
    }

    /// <summary>
    /// Settings for loading a <see cref="glTF"/> file.
    /// </summary>
    public sealed class GltfLoadOptions
    {
        private static readonly GltfLoadOptions defaultOptions = new GltfLoadOptions();

        /// <summary>
        /// Gets the options used when none are given: <see cref="GltfUriDecoding.Standard"/> uri decoding.
        /// </summary>
        public static GltfLoadOptions Default => defaultOptions;

        /// <summary>
        /// Gets how a relative buffer or image <c>uri</c> is turned into a file path. Defaults to
        /// <see cref="GltfUriDecoding.Standard"/>; choose <see cref="GltfUriDecoding.Literal"/> to restore the exact
        /// behavior of releases before this option existed.
        /// </summary>
        public GltfUriDecoding UriDecoding { get; init; } = GltfUriDecoding.Standard;
    }
}
