using GLTFRuntime;

namespace GLTF_Runtime_Loader.Tests
{
    /// <summary>
    /// Tests for a node's <c>matrix</c> property. glTF 2.0 section 5.25.4 (node.matrix): "A floating-point 4x4
    /// transformation matrix stored in column-major order." Section 3.5.3: "When matrix is defined, it MUST be
    /// decomposable to TRS properties." The glTF matrix multiplies column vectors (M * v), so the array's
    /// elements 12, 13 and 14 are the translation; a row-vector library such as MonoGame stores the transpose.
    /// <para>
    /// The fixture matrix is T(1,2,3) * Rz(90 degrees) * S(2,3,4). By hand, with p' = T * R * S * p: the point
    /// (1,0,0) goes to (1,4,3), (0,1,0) goes to (-2,2,3) and (0,0,1) goes to (1,2,7). Its column-major array is
    /// [0,2,0,0, -3,0,0,0, 0,0,4,0, 1,2,3,1].
    /// </para>
    /// </summary>
    public class NodeMatrixTests : IDisposable
    {
        private readonly string directory;

        public NodeMatrixTests()
        {
            directory = Path.Combine(Path.GetTempPath(), "gltf-runtime-loader-node-matrix-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        public void Dispose() => Directory.Delete(directory, recursive: true);

        private const string TrsMatrix = "[0,2,0,0, -3,0,0,0, 0,0,4,0, 1,2,3,1]";

        private glTF Load(string nodes, string extra = "")
        {
            string json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [], "bufferViews": [], "accessors": [],
              "nodes": [ {{nodes}} ]
              {{extra}}
            }
            """;
            File.WriteAllText(Path.Combine(directory, "test.gltf"), json);
            return new glTF(directory, "test.gltf");
        }

        /// <summary>Applies T * R * S to a point using column vectors, as the glTF specification defines.</summary>
        private static (float X, float Y, float Z) Apply(Node node, float x, float y, float z)
        {
            if (node.Scale is XYZ s) { x *= s.X; y *= s.Y; z *= s.Z; }
            if (node.Rotation is XYZW q)
            {
                // v' = v + 2 * cross(q.xyz, cross(q.xyz, v) + w * v)
                float cx = q.Y * z - q.Z * y + q.W * x;
                float cy = q.Z * x - q.X * z + q.W * y;
                float cz = q.X * y - q.Y * x + q.W * z;
                float rx = x + 2 * (q.Y * cz - q.Z * cy);
                float ry = y + 2 * (q.Z * cx - q.X * cz);
                float rz = z + 2 * (q.X * cy - q.Y * cx);
                x = rx; y = ry; z = rz;
            }
            if (node.Translation is XYZ t) { x += t.X; y += t.Y; z += t.Z; }
            return (x, y, z);
        }

        private static void AssertPoint((float X, float Y, float Z) actual, float x, float y, float z)
        {
            Assert.Equal(x, actual.X, 4);
            Assert.Equal(y, actual.Y, 4);
            Assert.Equal(z, actual.Z, 4);
        }

        [Fact]
        public void MatrixNodeDoesNotLoadAsIdentity()
        {
            Node node = Load($$"""{ "name": "m", "matrix": {{TrsMatrix}} }""").Nodes.Single();
            Assert.NotNull(node.Translation);
            Assert.NotNull(node.Rotation);
            Assert.NotNull(node.Scale);
        }

        [Fact]
        public void MatrixIsDecomposedIntoTranslationRotationAndScale()
        {
            Node node = Load($$"""{ "name": "m", "matrix": {{TrsMatrix}} }""").Nodes.Single();

            Assert.Equal(new XYZ(1f, 2f, 3f), node.Translation);
            Assert.Equal(2f, node.Scale!.Value.X, 4);
            Assert.Equal(3f, node.Scale.Value.Y, 4);
            Assert.Equal(4f, node.Scale.Value.Z, 4);
            float h = MathF.Sqrt(0.5f);
            Assert.Equal(0f, node.Rotation!.Value.X, 4);
            Assert.Equal(0f, node.Rotation.Value.Y, 4);
            Assert.Equal(h, node.Rotation.Value.Z, 4);
            Assert.Equal(h, node.Rotation.Value.W, 4);
        }

        [Fact]
        public void DecomposedTrsMapsPointsLikeTheColumnMajorMatrixDoes()
        {
            // A transposed read would put the translation at (0,0,0) and scramble these points.
            Node node = Load($$"""{ "name": "m", "matrix": {{TrsMatrix}} }""").Nodes.Single();

            AssertPoint(Apply(node, 1, 0, 0), 1, 4, 3);
            AssertPoint(Apply(node, 0, 1, 0), -2, 2, 3);
            AssertPoint(Apply(node, 0, 0, 1), 1, 2, 7);
            AssertPoint(Apply(node, 0, 0, 0), 1, 2, 3);
        }

        [Fact]
        public void MatrixWithARotationAboutXAndAnotherScaleDecomposesToo()
        {
            // T(-5,0.5,9) * Rx(90 degrees) * S(1,2,3): Rx(90) maps (x,y,z) to (x,-z,y).
            // (1,0,0) -> (1,0,0) -> (-4,0.5,9); (0,1,0) -> (0,2,0) -> (0,0,2) -> (-5,0.5,11); (0,0,1) -> (0,0,3) -> (0,-3,0) -> (-5,-2.5,9).
            Node node = Load("""{ "name": "m", "matrix": [1,0,0,0, 0,0,2,0, 0,-3,0,0, -5,0.5,9,1] }""").Nodes.Single();

            AssertPoint(Apply(node, 1, 0, 0), -4, 0.5f, 9);
            AssertPoint(Apply(node, 0, 1, 0), -5, 0.5f, 11);
            AssertPoint(Apply(node, 0, 0, 1), -5, -2.5f, 9);
        }

        [Fact]
        public void MirroredMatrixKeepsItsHandednessInTheScale()
        {
            // T(1,1,1) * S(-2,3,4): (1,0,0) -> (-1,1,1), (0,1,0) -> (1,4,1), (0,0,1) -> (1,1,5).
            Node node = Load("""{ "name": "m", "matrix": [-2,0,0,0, 0,3,0,0, 0,0,4,0, 1,1,1,1] }""").Nodes.Single();

            AssertPoint(Apply(node, 1, 0, 0), -1, 1, 1);
            AssertPoint(Apply(node, 0, 1, 0), 1, 4, 1);
            AssertPoint(Apply(node, 0, 0, 1), 1, 1, 5);
        }

        [Fact]
        public void MatrixThatIsNotSixteenNumbersThrows()
        {
            Assert.Throws<InvalidOperationException>(() => Load("""{ "name": "m", "matrix": [1,0,0,0, 0,1,0,0] }"""));
        }

        [Fact]
        public void IdentityMatrixDecomposesToTheIdentityTransform()
        {
            Node node = Load("""{ "name": "m", "matrix": [1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1] }""").Nodes.Single();
            AssertPoint(Apply(node, 3, 4, 5), 3, 4, 5);
        }

        [Fact]
        public void NodeWithNoMatrixAndNoTrsStillHasNoTransformProperties()
        {
            Node node = Load("""{ "name": "plain" }""").Nodes.Single();
            Assert.Null(node.Translation);
            Assert.Null(node.Rotation);
            Assert.Null(node.Scale);
        }

        [Fact]
        public void ExplicitTrsIsKeptWhenAMatrixIsAlsoPresent()
        {
            // glTF 2.0 section 3.5.3 allows a matrix or TRS properties, not both. Such a file loaded with its
            // TRS properties before matrix was read, so the TRS properties keep winning and matrix is ignored.
            Node node = Load($$"""{ "name": "both", "matrix": {{TrsMatrix}}, "translation": [7, 8, 9] }""").Nodes.Single();

            Assert.Equal(new XYZ(7f, 8f, 9f), node.Translation);
            Assert.Null(node.Rotation);
            Assert.Null(node.Scale);
        }

        [Fact]
        public void AnimationTargetingAMatrixNodeStillLoads()
        {
            // Section 3.5.3: "When a node is targeted for animation (referenced by an animation.channel.target),
            // only TRS properties MAY be present; matrix MUST NOT be present." A violating file is not rejected;
            // the matrix is decomposed like any other and the channel keeps its node.
            // 32 zero bytes as a data URI: two SCALAR times (8 bytes) and two VEC3 values (24 bytes).
            string zeros = new string('A', 40) + "AAA=";
            string json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "uri": "data:application/octet-stream;base64,{{zeros}}", "byteLength": 32 } ],
              "bufferViews": [ { "buffer": 0, "byteOffset": 0, "byteLength": 8 }, { "buffer": 0, "byteOffset": 8, "byteLength": 24 } ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 2, "type": "SCALAR" },
                { "bufferView": 1, "componentType": 5126, "count": 2, "type": "VEC3" }
              ],
              "animations": [ {
                "channels": [ { "sampler": 0, "target": { "node": 0, "path": "translation" } } ],
                "samplers": [ { "input": 0, "output": 1 } ]
              } ],
              "nodes": [ { "name": "m", "matrix": {{TrsMatrix}} } ]
            }
            """;
            File.WriteAllText(Path.Combine(directory, "test.gltf"), json);
            glTF result = new glTF(directory, "test.gltf");

            Node node = result.Nodes.Single();
            Assert.Same(node, result.Animations!.Single().Channels.Single().Target.Node);
            Assert.Equal(new XYZ(1f, 2f, 3f), node.Translation);
        }
    }
}
