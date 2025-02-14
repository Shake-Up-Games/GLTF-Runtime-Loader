using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Describes a scene defined in a GLTF file.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(Name)},nq}} ({{{nameof(Nodes)}.Count}})")]
    public class Scene
    {
        /// <summary>
        /// The user-defined name of this object.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Each root node.
        /// </summary>
        public ReadOnlyCollection<Node> Nodes { get; }

        internal Scene(JsonNode source, ReadOnlyCollection<Node> nodes)
        {
            Name = source["name"]?.GetValue<string>();
            Nodes = new((from n in source["nodes"]!.AsArray() select nodes[n.GetValue<int>()]).ToList());
        }
    }
}