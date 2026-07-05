namespace ShakeUpGames.Common3D
{
    /// <summary>
    /// An enumeration of primitive vertex attributes that may be used in the structures of a shader.
    /// </summary>
    public enum PrimitiveAttributes
    {
        /// <summary>
        /// The position attribute.
        /// </summary>
        POSITION,
        /// <summary>
        /// The normal attribute.
        /// </summary>
        NORMAL,
        /// <summary>
        /// The tangent attribute.
        /// <para>On a primitive's base attributes, this is a 4-component value (XYZ tangent plus a W sign for handedness). On a morph target, per the glTF 2.0 specification, this is a 3-component displacement with no W component.</para>
        /// </summary>
        TANGENT,
        /// <summary>
        /// A texture coordinate attribute.
        /// </summary>
        TEXCOORD,
        /// <summary>
        /// A joint indices attribute.
        /// </summary>
        JOINTS,
        /// <summary>
        /// A joint weights attribute.
        /// </summary>
        WEIGHTS
    }
}