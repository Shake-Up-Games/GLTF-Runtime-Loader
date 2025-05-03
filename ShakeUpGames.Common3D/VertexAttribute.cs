using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ShakeUpGames.Common3D
{
    /// <summary>
    /// Describes a vertex attribute for use in creating vertex elements.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    // "Attribute" also has special meaning for graphics programming
    public readonly struct VertexAttribute : IEquatable<VertexAttribute>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        /// <summary>
        /// The attribute type for this vertex attribute.
        /// </summary>
        public readonly PrimitiveAttributes Type { get; }

        /// <summary>
        /// The usage index for this vertex attribute, or null if none was specified.
        /// <para>For example, if the library specifies TEXCOORD_1, this is 1.</para>
        /// </summary>
        public readonly int? UsageIndex { get; }

        /// <summary>
        /// Constructs a new vertex attribute.
        /// </summary>
        /// <param name="type">The type of primitive this attribute represents.</param>
        /// <param name="usageIndex">An optional usage index.</param>
        public VertexAttribute(PrimitiveAttributes type, int? usageIndex)
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

        /// <summary>
        /// Returns whether <paramref name="obj"/> is a <see cref="VertexAttribute"/> instance with the same property values as this instance.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is VertexAttribute va)
                return Equals(va);
            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="a"/> is equal to <paramref name="b"/>.
        /// </summary>
        public static bool operator ==(VertexAttribute a, VertexAttribute b) => a.Equals(b);
        /// <summary>
        /// Returns whether <paramref name="a"/> is not equal to <paramref name="b"/>.
        /// </summary>
        public static bool operator !=(VertexAttribute a, VertexAttribute b) => !a.Equals(b);

        /// <summary>
        /// Returns a unique hash code for this <see cref="VertexAttribute"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Type.GetHashCode(), UsageIndex?.GetHashCode() ?? 0);
        }

        /// <summary>
        /// Returns whether <paramref name="other"/> has the same property values as this instance.
        /// </summary>
        public bool Equals(VertexAttribute other) => Type == other.Type && UsageIndex == other.UsageIndex;
    }
}