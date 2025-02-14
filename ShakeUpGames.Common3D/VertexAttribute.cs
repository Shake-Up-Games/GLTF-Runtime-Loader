using System.Diagnostics;

namespace ShakeUpGames.Common3D
{
    /// <summary>
    /// Describes a vertex attribute for use in creating vertex elements.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
    public readonly struct VertexAttribute
    {
        /// <summary>
        /// The attribute type for this vertex attribute.
        /// </summary>
        public readonly PrimitiveAttributes Type;

        /// <summary>
        /// The usage index for this vertex attribute, or null if none was specified.
        /// <para>For example, if the library specifies TEXCOORD_1, this is 1.</para>
        /// </summary>
        public readonly int? UsageIndex;

        /// <summary>
        /// Constructs a new vertex attribute.
        /// </summary>
        /// <param name="type">The type of primitive this attribute represents.</param>
        /// <param name="usageIndex">An optional usage index.</param>
        internal VertexAttribute(PrimitiveAttributes type, int? usageIndex)
        {
            Type = type;
            UsageIndex = usageIndex;
        }

        /// <summary>
        /// Returns a human-friendly string representation of this attribute.
        /// </summary>
        public override string ToString()
        {
            if (UsageIndex == null)
                return Type.ToString();
            else
                return $"{Type}_{UsageIndex}";
        }
    }
}