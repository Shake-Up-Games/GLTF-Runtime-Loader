using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// The descriptor of the animated property.
    /// </summary>
    public class ChannelTarget
    {
        /// <summary>
        /// The node to animate. When undefined, the animated object MAY be defined by an extension.
        /// </summary>
        public Node? Node { get; }

        /// <summary>
        /// The name of the node’s TRS property to animate, or the "weights" of the Morph Targets it instantiates.
        /// <list type="bullet">
        /// <item>For the "translation" property, the values that are provided by the sampler are the translation along the X, Y, and Z axes.</item>
        /// <item>For the "rotation" property, the values are a quaternion in the order (x, y, z, w), where w is the scalar.</item>
        /// <item>For the "scale" property, the values are the scaling factors along the X, Y, and Z axes.</item>
        /// </list>
        /// </summary>
        public string Path { get; }

        internal ChannelTarget(JsonNode source, ReadOnlyCollection<Node> nodes)
        {
            var nodeIndex = source["node"]?.GetValue<int>();
            if (nodeIndex.HasValue)
                Node = nodes[nodeIndex.Value];

            Path = source["path"]!.GetValue<string>();
        }
    }
}