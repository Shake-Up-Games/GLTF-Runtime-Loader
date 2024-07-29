using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// An animation channel combines an animation sampler with a target property being animated.
    /// </summary>
    public sealed class Channel
    {
        /// <summary>
        /// A sampler in this animation used to compute the value for the target.
        /// </summary>
        public AnimationSampler Sampler { get; }

        /// <summary>
        /// The descriptor of the animated property.
        /// </summary>
        public ChannelTarget Target { get; }

        internal Channel(JsonNode source, ReadOnlyCollection<Node> nodes, ReadOnlyCollection<AnimationSampler> samplers)
        {
            Sampler = samplers[source["sampler"]!.GetValue<int>()];
            Target = new ChannelTarget(source["target"]!, nodes);
        }
    }
}