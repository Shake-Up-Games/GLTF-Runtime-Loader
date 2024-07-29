﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GLTFRuntime
{
    /// <summary>
    /// A vector that holds 3 values.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(X)}}}, {{{nameof(Y)}}}, {{{nameof(Z)}}}, {{{nameof(W)}}}")]
    public readonly struct XYZW : IEquatable<XYZW>
    {
        /// <summary>
        /// The X component.
        /// </summary>
        public float X { get; }
        /// <summary>
        /// The Y component.
        /// </summary>
        public float Y { get; }
        /// <summary>
        /// The Z component.
        /// </summary>
        public float Z { get; }
        /// <summary>
        /// The W component.
        /// </summary>
        public float W { get; }

        /// <summary>
        /// Constructs a new vector from an array of 4 values.
        /// </summary>
        public XYZW(float[] array)
        {
            ArgumentNullException.ThrowIfNull(array, nameof(array));
            if (array.Length != 4)
                throw new ArgumentException($"{nameof(array)} must have a length of 4.");
            X = array[0];
            Y = array[1];
            Z = array[2];
            W = array[3];
        }

        /// <summary>
        /// Constructs a new vector with specific values.
        /// </summary>
        public XYZW(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Constructs a new vector with zeroed values.
        /// </summary>
        public XYZW()
            : this(0f, 0f, 0f, 0f)
        {

        }

        /// <summary>
        /// Converts an array of 3 float values to a vector.
        /// </summary>
        public static XYZW FromArray(float[] array)
        {
            ArgumentNullException.ThrowIfNull(array, nameof(array));
            return new XYZW(array[0], array[1], array[2], array[3]);
        }


        /// <summary>
        /// Return whether two vectors are equal.
        /// </summary>        
        public static bool operator ==(XYZW v1, XYZW v2)
        {
            return v1.Equals(v2);
        }

        /// <summary>
        /// Return whether two vectors are not equal.
        /// </summary>        
        public static bool operator !=(XYZW v1, XYZW v2)
        {
            return v1.Equals(v2);
        }

        /// <summary>
        /// Returns whether <paramref name="obj"/> equals this vector.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null)
                return false;
            else if (obj is XYZW other)
                return Equals(other);
            else
                return false;
        }

        /// <summary>
        /// Returns whether <paramref name="other"/> equals this vector.
        /// </summary>
        public bool Equals(XYZW other)
        {
            return other.X == X && other.Y == Y && other.Z == Z && other.W == W;
        }

        /// <summary>
        /// Returns a unique hashcode for this object.
        /// </summary>
        public override int GetHashCode()
        {
            // Multiply this by 3 big primes and get the hashcode of that sum.
            return (1193f * X + 1201f * Y + 1213f * Z * 1217 * W).GetHashCode();
        }
    }
}