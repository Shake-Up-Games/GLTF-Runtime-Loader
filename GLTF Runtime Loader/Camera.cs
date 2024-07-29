using System.Diagnostics;
using System.Numerics;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace GLTFRuntime
{
    /// <summary>
    /// Specifies if the camera uses a perspective or orthographic projection.
    /// </summary>
    public enum CameraType
    {
        /// <summary>
        /// The camera uses a perspective projection.
        /// </summary>
        perspective,
        /// <summary>
        /// /// The camera uses an orthographic projection.
        /// </summary>
        orthographic
    }

    /// <summary>
    /// A camera’s projection.A <see cref="Node"/> MAY reference a camera to apply a transform to place the camera in the scene.
    /// </summary>
        [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public abstract class Camera
    {
        /// <summary>
        /// The user-defined name of this object.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Specifies if the camera uses a perspective or orthographic projection.
        /// </summary>
        public CameraType Type { get; }

        internal Camera(JsonNode source)
        {
            Name = source["name"]?.GetValue<string>();
            Type = Enum.Parse<CameraType>(source["type"]!.GetValue<string>());
        }

        /// <summary>
        /// Creates a <see cref="OrthographicCamera"/> or a <see cref="PerspectiveCamera"/> from the given node.
        /// </summary>
        internal static Camera Create(JsonNode source)
        {
            string type = source["type"]!.GetValue<string>();
            return type switch
            {
                "perspective" => new PerspectiveCamera(source),
                "orthographic" => new OrthographicCamera(source),
                _ => throw new NotImplementedException(),
            };
        }

        private string GetDebuggerDisplay()
        {
            return Name ?? $"{GetType()}";
        }
    }

    /// <summary>
    /// An orthographic camera containing properties to create an orthographic projection matrix.
    /// </summary>
    public sealed class OrthographicCamera : Camera
    {
        /// <summary>
        /// The floating-point horizontal magnification of the view. This value MUST NOT be equal to zero. This value SHOULD NOT be negative.
        /// </summary>
        public float XMag { get; }

        /// <summary>
        /// The floating-point vertical magnification of the view. This value MUST NOT be equal to zero. This value SHOULD NOT be negative.The floating-point vertical magnification of the view. This value MUST NOT be equal to zero. This value SHOULD NOT be negative.
        /// </summary>
        public float YMag { get; }

        /// <summary>
        /// The floating-point distance to the far clipping plane. This value MUST NOT be equal to zero. zfar MUST be greater than znear.
        /// </summary>
        public float ZFar { get; }

        /// <summary>
        /// The floating-point distance to the near clipping plane.
        /// </summary>
        public float ZNear { get; }

        internal OrthographicCamera(JsonNode source)
            : base(source)
        {
            XMag = source["xmag"]!.GetValue<float>();
            YMag = source["ymag"]!.GetValue<float>();
            ZFar = source["zfar"]!.GetValue<float>();
            ZNear = source["znear"]!.GetValue<float>();
        }
    }

    /// <summary>
    /// A perspective camera containing properties to create a perspective projection matrix.
    /// </summary>
    public sealed class PerspectiveCamera : Camera
    {
        /// <summary>
        /// The floating-point aspect ratio of the field of view.
        /// </summary>
        public float? AspectRatio { get; }

        /// <summary>
        /// The floating-point vertical field of view in radians. This value SHOULD be less than π.
        /// </summary>
        public float YFOV { get; }

        /// <summary>
        /// The floating-point distance to the far clipping plane.
        /// </summary>
        public float? ZFar { get; }

        /// <summary>
        /// The floating-point distance to the near clipping plane.
        /// </summary>
        public float ZNear { get; }

        internal PerspectiveCamera(JsonNode source)
            : base(source)
        {
            AspectRatio = source["aspectRatio"]?.GetValue<float>();
            YFOV = source["yfov"]!.GetValue<float>();
            ZFar = source["zfar"]?.GetValue<float>();
            ZNear = source["znear"]!.GetValue<float>();
        }
    }
}