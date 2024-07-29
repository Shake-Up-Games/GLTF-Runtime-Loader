using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// A set of primitives to be rendered.Its global transform is defined by a node that references it.
    /// </summary>    
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public class Mesh
    {
        /// <summary>
        /// An array of primitives, each defining geometry to be rendered.
        /// </summary>
        public Primitive[] Primitives { get; }

        /// <summary>
        /// Array of weights to be applied to the morph targets. The number of array elements MUST match the number of morph targets.
        /// </summary>
        public float[]? Weights { get; }

        /// <summary>
        /// The user-defined name of this object.
        /// </summary>
        public string? Name { get; }

        internal Mesh(JsonNode source, Accessor[] accessors, ReadOnlyCollection<Material> materials)
        {
            Name = source["name"]?.GetValue<string>();
            var weightsArray = source["weights"];
            if (weightsArray != null)
                Weights = GLTFHelpers.GetDataFromArray<float>(weightsArray.AsArray());

            JsonArray primitiveNodes = source["primitives"]!.AsArray();
            Primitives = new Primitive[primitiveNodes.Count];
            for (int i = 0; i < primitiveNodes.Count; i++)
                Primitives[i] = new Primitive(primitiveNodes[i]!, accessors, materials);
        }

        private string GetDebuggerDisplay()
        {
            if (Name == null)
                return $"{Primitives.Length} Primitive{(Primitives.Length == 1 ? "" : "s")}";
            else
                return $"{Name} ({Primitives.Length} Primitive{(Primitives.Length == 1 ? "" : "s")})";
        }
    }
}