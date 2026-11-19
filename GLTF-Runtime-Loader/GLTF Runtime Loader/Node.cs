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
    /// <para>
    /// This class exposes only TRS. A node's <c>matrix</c> is decomposed into <see cref="Translation"/>,
    /// <see cref="Rotation"/> and <see cref="Scale"/> when the node is read, so consumers have a single source.
    /// The relevant glTF 2.0 clauses, quoted from section 3.5.3 (Transformations) and 5.25.4 (node.matrix):
    /// <list type="bullet">
    /// <item>"Any node MAY define a local space transform either by supplying a matrix property, or any of translation, rotation, and scale properties (also known as TRS properties)."</item>
    /// <item>"When matrix is defined, it MUST be decomposable to TRS properties."</item>
    /// <item>"Transformation matrices cannot skew or shear."</item>
    /// <item>"When a node is targeted for animation (referenced by an animation.channel.target), only TRS properties MAY be present; matrix MUST NOT be present."</item>
    /// <item>"A floating-point 4x4 transformation matrix stored in column-major order."</item>
    /// </list>
    /// A file that breaks the "either" or the animation rule is not rejected. If a node has both a matrix and
    /// any TRS property, the TRS properties are used exactly as written and the matrix is ignored. A node that
    /// has a matrix and is an animation target gets the decomposed TRS values as its static transform; the
    /// animation channels then drive TRS as for any other node. A matrix that is not 16 numbers is an error,
    /// since the schema requires exactly 16.
    /// </para>
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

        /// <summary>
        /// The index of the mesh this node references in the file's "meshes" array (see <see cref="glTF.MeshList"/>),
        /// or null if this node has no mesh.
        /// </summary>
        public int? MeshIndex { get; }

        /// <summary>
        /// Gets this node's parent, or null if it has no parent.
        /// </summary>
        public Node? Parent { get; internal set; }
        internal bool NoTransform => Translation == null && Scale == null && Rotation == null;

        /// <summary>
        /// Gets the inverse bind matrices for this joint node, for each of the skins in the library.
        /// </summary>
        public Dictionary<int, ReadOnlyCollection<float>> InverseBindMatrices { get; internal set; } = new Dictionary<int, ReadOnlyCollection<float>>();

        internal Node(JsonNode source)
        {
            Name = source["name"]!.GetValue<string>();
            Index = source.GetElementIndex();

            var meshNode = source["mesh"];
            if (meshNode != null)
                MeshIndex = meshNode.GetValue<int>();

            var rotNode = source["rotation"];
            if (rotNode != null)
                Rotation = XYZW.FromArray(GLTFHelpers.GetDataFromArray<float>(rotNode.AsArray()));
            var transNode = source["translation"];
            if (transNode != null)
                Translation = XYZ.FromArray(GLTFHelpers.GetDataFromArray<float>(transNode.AsArray()));
            var scaleNode = source["scale"];
            if (scaleNode != null)
                Scale = XYZ.FromArray(GLTFHelpers.GetDataFromArray<float>(scaleNode.AsArray()));

            // A "matrix" is decomposed into the TRS properties, so consumers only ever read those. When the
            // file also supplies explicit TRS properties (not allowed by section 3.5.3, see the class
            // remarks), they stay as written and the matrix is ignored, which is what this class did before
            // it read matrix at all.
            var matrixNode = source["matrix"];
            if (matrixNode != null && rotNode == null && transNode == null && scaleNode == null)
            {
                float[] matrix = GLTFHelpers.GetDataFromArray<float>(matrixNode.AsArray());
                if (matrix.Length != 16)
                    throw new InvalidOperationException($"Node \"{Name}\" has a matrix with {matrix.Length} elements; glTF 2.0 requires exactly 16.");
                Decompose(matrix, out XYZ translation, out XYZW rotation, out XYZ scale);
                Translation = translation;
                Rotation = rotation;
                Scale = scale;
            }

            // Children should be assigned by the caller after all nodes are instantiated.
        }

        /// <summary>
        /// Splits a glTF <c>matrix</c> into translation, rotation and scale.
        /// <para>
        /// Matrix order: glTF stores the 16 floats in column-major order and multiplies column vectors
        /// (<c>M * v</c>), so element <c>m[4 * column + row]</c> is the matrix entry at that row and column,
        /// the first three elements are the image of the X axis, and the translation is <c>m[12..14]</c>.
        /// A row-vector library such as MonoGame (<c>v * M</c>) lays the same 16 floats out as the
        /// transpose, with the translation in its fourth row; this method reads glTF's convention only and
        /// involves no such type.
        /// </para>
        /// <para>
        /// With <c>M = T * R * S</c>, column <c>c</c> of the upper 3x3 is <c>scale[c]</c> times column <c>c</c>
        /// of the rotation, so each scale is the length of its column. A negative determinant (a mirror) is
        /// folded into the X scale. The spec requires the matrix to be decomposable ("Transformation matrices
        /// cannot skew or shear"); a skewed matrix is decomposed on a best-effort basis rather than rejected.
        /// </para>
        /// </summary>
        internal static void Decompose(float[] m, out XYZ translation, out XYZW rotation, out XYZ scale)
        {
            // Columns of the upper 3x3 (column-major: column c starts at m[4 * c]).
            double c0x = m[0], c0y = m[1], c0z = m[2];
            double c1x = m[4], c1y = m[5], c1z = m[6];
            double c2x = m[8], c2y = m[9], c2z = m[10];

            double sx = Math.Sqrt(c0x * c0x + c0y * c0y + c0z * c0z);
            double sy = Math.Sqrt(c1x * c1x + c1y * c1y + c1z * c1z);
            double sz = Math.Sqrt(c2x * c2x + c2y * c2y + c2z * c2z);

            // determinant = c0 . (c1 x c2)
            double det = c0x * (c1y * c2z - c1z * c2y)
                       - c0y * (c1x * c2z - c1z * c2x)
                       + c0z * (c1x * c2y - c1y * c2x);
            if (det < 0)
                sx = -sx;

            translation = new XYZ(m[12], m[13], m[14]);
            scale = new XYZ((float)sx, (float)sy, (float)sz);

            if (sx == 0 || sy == 0 || sz == 0)
            {
                // A collapsed axis leaves the rotation undefined; use the identity.
                rotation = new XYZW(0f, 0f, 0f, 1f);
                return;
            }

            // Rotation matrix entries r_<row><column>, from the columns divided by their scales.
            double r00 = c0x / sx, r10 = c0y / sx, r20 = c0z / sx;
            double r01 = c1x / sy, r11 = c1y / sy, r21 = c1z / sy;
            double r02 = c2x / sz, r12 = c2y / sz, r22 = c2z / sz;

            double x, y, z, w;
            double trace = r00 + r11 + r22;
            if (trace > 0)
            {
                double s = Math.Sqrt(trace + 1.0) * 2.0;
                w = 0.25 * s;
                x = (r21 - r12) / s;
                y = (r02 - r20) / s;
                z = (r10 - r01) / s;
            }
            else if (r00 > r11 && r00 > r22)
            {
                double s = Math.Sqrt(1.0 + r00 - r11 - r22) * 2.0;
                w = (r21 - r12) / s;
                x = 0.25 * s;
                y = (r01 + r10) / s;
                z = (r02 + r20) / s;
            }
            else if (r11 > r22)
            {
                double s = Math.Sqrt(1.0 + r11 - r00 - r22) * 2.0;
                w = (r02 - r20) / s;
                x = (r01 + r10) / s;
                y = 0.25 * s;
                z = (r12 + r21) / s;
            }
            else
            {
                double s = Math.Sqrt(1.0 + r22 - r00 - r11) * 2.0;
                w = (r10 - r01) / s;
                x = (r02 + r20) / s;
                y = (r12 + r21) / s;
                z = 0.25 * s;
            }

            double length = Math.Sqrt(x * x + y * y + z * z + w * w);
            rotation = new XYZW((float)(x / length), (float)(y / length), (float)(z / length), (float)(w / length));
        }
    }
}