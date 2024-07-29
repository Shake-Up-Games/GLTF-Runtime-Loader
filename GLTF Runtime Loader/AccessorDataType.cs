namespace GLTFRuntime
{
    [System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    sealed class ComponentCountAttribute : Attribute
    {
        public int ComponentCount { get; }

        public ComponentCountAttribute(int componentCount)
        {
            ComponentCount = componentCount;
        }
    }
    /// <summary>
    /// Named values representing the types of data that accessors refer to in JSON.
    /// </summary>
    public enum AccessorDataType
    {
        /// <summary>
        /// SCALAR (1 component)
        /// </summary>
        [ComponentCount(1)]
        SCALAR,
        /// <summary>
        /// VEC2 (2 components)
        /// </summary>
        [ComponentCount(2)]
        VEC2,
        /// <summary>
        /// VEC3 (3 components)
        /// </summary>
        [ComponentCount(3)]
        VEC3,
        /// <summary>
        /// VEC4 (4 components)
        /// </summary>
        [ComponentCount(4)]
        VEC4,
        /// <summary>
        /// MAT2 (4 components)
        /// </summary>
        [ComponentCount(4)]
        MAT2,
        /// <summary>
        /// MAT3 (9 components)
        /// </summary>
        [ComponentCount(9)]
        MAT3,
        /// <summary>
        /// MAT4 (16 components)
        /// </summary>
        [ComponentCount(16)]
        MAT4
    }
}