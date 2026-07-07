namespace ShakeUpGames.Common3D
{
    /// <summary>
    /// An enumeration of primitive vertex attributes that may be used in the structures of a shader.
    /// </summary>
    /// <remarks>
    /// Explicit numeric values, deliberately: this enum is consumed across an assembly boundary
    /// (GLTF Runtime Loader is compiled against a specific ShakeUpGames.Common3D version, but the
    /// actual assembly loaded at a consumer's runtime is resolved independently via NuGet). Implicit
    /// sequential values previously let inserting TANGENT in the middle silently shift every
    /// subsequent member's numeric value - harmless if every assembly involved is rebuilt together,
    /// but a real, hard-to-diagnose bug the moment a consumer's dependency resolution picks up a
    /// GLTF Runtime Loader build and a Common3D build from different points in time (exactly what
    /// happened: JOINTS/WEIGHTS switch cases compiled against post-TANGENT numeric values, evaluated
    /// at runtime against a pre-TANGENT-published Common3D whose members still held their old values).
    /// Fixing the immediate mismatch by republishing a consistent pair of packages doesn't prevent
    /// recurrence the next time either assembly changes independently - explicit values do.
    /// </remarks>
    public enum PrimitiveAttributes
    {
        /// <summary>
        /// The position attribute.
        /// </summary>
        POSITION = 0,
        /// <summary>
        /// The normal attribute.
        /// </summary>
        NORMAL = 1,
        /// <summary>
        /// The tangent attribute.
        /// <para>On a primitive's base attributes, this is a 4-component value (XYZ tangent plus a W sign for handedness). On a morph target, per the glTF 2.0 specification, this is a 3-component displacement with no W component.</para>
        /// </summary>
        TANGENT = 2,
        /// <summary>
        /// A texture coordinate attribute.
        /// </summary>
        TEXCOORD = 3,
        /// <summary>
        /// A joint indices attribute.
        /// </summary>
        JOINTS = 4,
        /// <summary>
        /// A joint weights attribute.
        /// </summary>
        WEIGHTS = 5
    }
}