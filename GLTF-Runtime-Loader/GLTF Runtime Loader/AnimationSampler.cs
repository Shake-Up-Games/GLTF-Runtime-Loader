using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Allowed values for <see cref="AnimationSampler.Interpolation"/>.
    /// </summary>
    public enum Interpolation
    {
        /// <summary>
        /// The animated values are linearly interpolated between keyframes. When targeting a rotation, spherical linear interpolation (slerp) SHOULD be used to interpolate quaternions. The number of output elements MUST equal the number of input elements.
        /// </summary>
        LINEAR,
        /// <summary>
        /// The animated values remain constant to the output of the first keyframe, until the next keyframe. The number of output elements MUST equal the number of input elements.
        /// </summary>
        STEP,
        /// <summary>
        /// The animation’s interpolation is computed using a cubic spline with specified tangents. The number of output elements MUST equal three times the number of input elements. For each input element, the output stores three elements, an in-tangent, a spline vertex, and an out-tangent. There MUST be at least two keyframes when using this interpolation.
        /// </summary>
        CUBICSPLINE
    }

    /// <summary>
    /// An animation sampler combines timestamps with a sequence of output values and defines an interpolation algorithm.
    /// </summary>
    public class AnimationSampler
    {
        /// <summary>
        /// Keyframe timestamps.
        /// </summary>
        public ReadOnlyCollection<float> Input { get; }

        /// <summary>
        /// Interpolation algorithm.
        /// </summary>
        public Interpolation Interpolation { get; }

        /// <summary>
        /// Keyframe output values.
        /// <para>What type of object this collection holds depends on <see cref="Interpolation"/>. See documentation on <see cref="GLTFRuntime.Interpolation"/> members.</para>
        /// </summary>
        public ReadOnlyCollection<ReadOnlyCollection<object>> Output { get; }

        internal AnimationSampler(JsonNode source, Accessor[] accessors)
        {
            Input = new ReadOnlyCollection<float>(
                (
                from inner in
                accessors[source["input"]!.GetValue<int>()].Data
                select (float)inner[0]
            ).ToList()
            );

            JsonNode? interpolationNode = source["interpolation"];
            Interpolation value;
            if (interpolationNode == null || !Enum.TryParse<Interpolation>(interpolationNode.GetValue<string>(), out value))
                value = Interpolation.LINEAR;
            Interpolation = value;

            Output = accessors[source["output"]!.GetValue<int>()].Data;
        }
    }
}