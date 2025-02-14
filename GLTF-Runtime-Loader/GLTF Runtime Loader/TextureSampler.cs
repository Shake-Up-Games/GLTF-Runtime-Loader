using System.Text.Json.Nodes;

namespace GLTFRuntime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Identifiers should not contain underscores
    /// <summary>
    /// Allowed values for <see cref="TextureSampler.MagFilter"/>.
    /// </summary>
    public enum MagFilter
    {
        None = 0,
        NEAREST = 9728,
        LINEAR = 9729
    }

    /// <summary>
    /// Allowed values for <see cref="TextureSampler.MinFilter"/>
    /// </summary>
    public enum MinFilter
    {
        None = 0,
        NEAREST = 9728,
        LINEAR = 9729,
        NEAREST_MIPMAP_NEAREST = 9984,
        LINEAR_MIPMAP_NEAREST = 9985,
        NEAREST_MIPMAP_LINEAR = 9986,
        LINEAR_MIPMAP_LINEAR = 9987,
    }

    /// <summary>
    /// Allowed values for <see cref="TextureSampler.WrapS"/> and <see cref="TextureSampler.WrapT"/>
    /// </summary>
    public enum Wrap
    {
        None,
        CLAMP_TO_EDGE = 33071,
        MIRRORED_REPEAT = 33648,
        REPEAT = 10497
    }

#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Texture sampler properties for filtering and wrapping modes.
    /// </summary>
    public class TextureSampler
    {
        /// <summary>
        /// Magnification filter.
        /// </summary>
        public MagFilter MagFilter { get; }

        // Make an enum for MinFilter and wrapS and wrapT fields

        /// <summary>
        /// Minification filter.
        /// </summary>
        public MinFilter MinFilter { get; }

        /// <summary>
        /// S (U) wrapping mode.
        /// </summary>
        public Wrap WrapS { get; }

        /// <summary>
        /// T (V) wrapping mode.
        /// </summary>
        public Wrap WrapT { get; }

        /// <summary>
        /// The user-defined name of this object.
        /// </summary>
        public string? Name { get; }

        internal TextureSampler(JsonNode source)
        {
            MagFilter = (MagFilter)(GLTFHelpers.ExtractInt(source, "magFilter") ?? 0);
            MinFilter = (MinFilter)(GLTFHelpers.ExtractInt(source, "minFilter") ?? 0);
            WrapS = (Wrap)(GLTFHelpers.ExtractInt(source, "wrapS") ?? 10497);
            WrapT = (Wrap)(GLTFHelpers.ExtractInt(source, "wrapT") ?? 10497);

            Name = source["name"]?.GetValue<string>();
        }
    }
}