#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CA1008 // Enums should have zero value

namespace GLTFRuntime
{
    /// <summary>
    /// Named values indicating the intended GPU buffer type to use with a buffer view.
    /// <para>See <see href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#_bufferview_target">Khronos Documentation</see>.</para>
    /// </summary>
    public enum BufferViewTarget
    {
        /// <summary>
        /// Vertex attributes
        /// </summary>
        ARRAY_BUFFER = 34962,
        /// <summary>
        /// Vertex array indices
        /// </summary>
        ELEMENT_ARRAY_BUFFER = 34963
    }
}

#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore CA1008 // Enums should have zero value