using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;
namespace GLTFRuntime
{
    /// <summary>
    /// A node in the node hierarchy.
    /// When the node contains skin, all mesh.primitives MUST contain JOINTS_0 and WEIGHTS_0 attributes.
    /// A node MAY have either a matrix or any combination of translation/rotation/scale (TRS) properties.
    /// TRS properties are converted to matrices and postmultiplied in the T * R* S order to compose the transformation matrix;
    /// first the scale is applied to the vertices, then the rotation, and then the translation.
    /// If none are provided, the transform is the identity.When a node is targeted for animation (referenced by an animation.channel.target),
    /// matrix MUST NOT be present.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(Name)},nq}}")]
    public class Node
    {
        /// <summary>
        /// The child nodes of this node.
        /// </summary>
        public ReadOnlyCollection<Node>? Children { get; internal set; }

        /// <summary>
        /// The name of this node.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The rotation of this node.
        /// </summary>
        public XYZW? Rotation { get; }

        /// <summary>
        /// The translation of this node.
        /// </summary>
        public XYZ? Translation { get; }

        /// <summary>
        /// The scale of this node.
        /// </summary>
        public XYZ? Scale { get; }

        /// <summary>
        /// The index of this node in the file's "nodes" array.
        /// </summary>
        public int Index { get; }
        internal Node(JsonNode source)
        {
            Name = source["name"]!.GetValue<string>();
            Index = source.GetElementIndex();

            var rotNode = source["rotation"];
            if (rotNode != null)
                Rotation = XYZW.FromArray(GLTFHelpers.GetDataFromArray<float>(rotNode.AsArray()));
            var transNode = source["translation"];
            if (transNode != null)
                Translation = XYZ.FromArray(GLTFHelpers.GetDataFromArray<float>(transNode.AsArray()));
            var scaleNode = source["scale"];
            if (scaleNode != null)
                Scale = XYZ.FromArray(GLTFHelpers.GetDataFromArray<float>(scaleNode.AsArray()));

            // Children should be assigned by the caller after all nodes are instantiated.
        }
    }
}