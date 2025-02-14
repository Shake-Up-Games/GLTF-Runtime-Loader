using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// A keyframe animation.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public sealed class Animation
    {
        /// <summary>
        /// A read-only collection of animation channels. 
        /// An animation channel combines an animation sampler with a target property being animated. 
        /// Different channels of the same animation MUST NOT have the same targets.
        /// </summary>
        public ReadOnlyCollection<Channel> Channels { get; }

        /// <summary>
        /// A read-only collection of animation samplers.
        /// An animation sampler combines timestamps with a sequence of output values and defines an interpolation algorithm.
        /// </summary>
        public ReadOnlyCollection<AnimationSampler> Samplers { get; }

        /// <summary>
        /// The user-defined name of this object.
        /// </summary>
        public string? Name { get; }

        internal Animation(JsonNode source, ReadOnlyCollection<Node> nodes, Accessor[] accessors)
        {
            Samplers = new ReadOnlyCollection<AnimationSampler>((from s in source["samplers"]!.AsArray() select new AnimationSampler(s, accessors)).ToList());
            Channels = new ReadOnlyCollection<Channel>((from c in source["channels"]!.AsArray() select new Channel(c, nodes, Samplers)).ToList());

            Name = source["name"]?.GetValue<string>();
        }

        private string GetDebuggerDisplay()
        {
            if (Name != null)
                return Name;
            else
                return $"{Channels.Count} channel{GLTFHelpers.PluralS(Channels.Count)}, {Samplers.Count} sampler{GLTFHelpers.PluralS(Samplers.Count)}";
        }
    }
}