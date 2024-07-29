using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Joints and matrices defining a skin.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public sealed class Skin
    {
        /// <summary>
        /// A collection of collections of 16 floats,
        /// each of which represents a 4x4 inverse bind matrix for the joint matching its index in the file's nodes array.
        /// <para>See <see cref="Node.Index"/> for matching inverse bind matrices with joints.</para>
        /// </summary>
        public ReadOnlyCollection<ReadOnlyCollection<float>>? InverseBindMatrices { get; }

        /// <summary>
        /// The index of the node used as a skeleton root.
        /// </summary>
        public Node? Skeleton { get; }

        /// <summary>
        /// Indices of skeleton nodes, used as joints in this skin.
        /// </summary>
        public ReadOnlyCollection<Node> Joints { get; }

        /// <summary>
        /// The user-defined name of this object.
        /// </summary>
        public string? Name { get; }

        internal Skin(JsonNode source, ReadOnlyCollection<Node> nodes, Accessor[] accessors)
        {
            Name = source["name"]?.GetValue<string>();

            var ibmNode = source["inverseBindMatrices"];
            if (ibmNode != null)
            {
                var ibmIndex = ibmNode.GetValue<int>();
                var data = accessors[ibmIndex].Data;
                InverseBindMatrices = new ReadOnlyCollection<ReadOnlyCollection<float>>(
                    (from matrix in data
                     select new ReadOnlyCollection<float>((from value in matrix select (float)value).ToList())
                    ).ToList()
                    );
            }

            var jointIndices = GLTFHelpers.GetDataFromArray<int>(source["joints"]!.AsArray());
            Joints = new ReadOnlyCollection<Node>((from index in jointIndices select nodes[index]).ToList());
        }

        private string GetDebuggerDisplay()
        {
            if (Name == null)
                return $"{Joints.Count} joint{GLTFHelpers.PluralS(Joints.Count)}";
            else
                return $"{Name}: {Joints.Count} joint{GLTFHelpers.PluralS(Joints.Count)}";
        }
    }
}