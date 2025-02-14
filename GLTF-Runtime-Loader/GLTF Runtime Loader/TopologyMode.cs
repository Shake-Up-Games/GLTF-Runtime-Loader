namespace GLTFRuntime
{
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    // The schema doesn't give any explanation other than the enum names.

    /// <summary>
    /// The topology type of primitives to render.
    /// </summary>
    public enum TopologyMode
    {
        POINTS = 0,
        LINES = 1,
        LINE_LOOP = 2,
        LINE_STRIP = 3,
        TRIANGLES = 4,
        TRIANGLE_STRIP = 5,
        TRIANGLE_FAN = 6
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CA1707 // Identifiers should not contain underscores
}