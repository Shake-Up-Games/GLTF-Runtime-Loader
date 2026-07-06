using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Joints and matrices defining a skin.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
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

            var jointIndices = GLTFHelpers.GetDataFromArray<int>(source["joints"]!.AsArray());
            Joints = new ReadOnlyCollection<Node>((from index in jointIndices select nodes[index]).ToList());

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

                int skinIndex = source.GetElementIndex();
                for (int i = 0; i < jointIndices.Length; i++)
                {
                    nodes[jointIndices[i]].InverseBindMatrices[skinIndex] = InverseBindMatrices[i];
                }
            }

            int? skeleton = GLTFHelpers.ExtractInt(source, "skeleton");
            if (skeleton.HasValue)
                Skeleton = Joints[skeleton.Value];
            else
                // No explicit "skeleton" index: infer the root as the joint with no ancestor
                // *within this skin's own joint list* - i.e. the topmost bone of the hierarchy this
                // skin actually uses. Checking "j.Parent.NoTransform" (whether the parent node
                // happens to have an identity transform) is not a valid signal for this at all: any
                // ordinary bone chain where a child bone's head sits exactly at its parent's local
                // origin (an extremely common, unremarkable rig - e.g. two bones placed end-to-end
                // with no offset) gives more than one joint an identity-transform parent, which used
                // to make this Single() throw on real content that has nothing wrong with it.
                Skeleton = Joints.SingleOrDefault(j => j.Parent is null || !Joints.Contains(j.Parent));
        }

        /// <summary>
        /// Returns a human-friendly string representation of this skin.
        /// </summary>
        public override string ToString()
        {
            if (Name == null)
                return $"{Joints.Count} joint{GLTFHelpers.PluralS(Joints.Count)}";
            else
                return $"{Name}: {Joints.Count} joint{GLTFHelpers.PluralS(Joints.Count)}";
        }
    }
}