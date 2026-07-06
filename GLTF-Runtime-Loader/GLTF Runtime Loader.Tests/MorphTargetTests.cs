using GLTFRuntime;
using ShakeUpGames.Common3D;
using System.Text.Json.Nodes;

namespace GLTF_Runtime_Loader.Tests
{
    public class MorphTargetTests
    {
        /// <summary>
        /// Builds a dense VEC3-float accessor with the given per-vertex values (each a 3-float row), backed by its own temp buffer.
        /// </summary>
        private static (Accessor accessor, TestBuffer buffer) Vec3Accessor(params float[][] rows)
        {
            byte[] bytes = new byte[rows.Length * 3 * sizeof(float)];
            System.Buffer.BlockCopy(rows.SelectMany(r => r).ToArray(), 0, bytes, 0, bytes.Length);
            var buffer = new TestBuffer(bytes, bytes.Length);
            var view = buffer.View(0, bytes.Length);
            JsonNode node = JsonNode.Parse($$"""{ "bufferView": 0, "componentType": 5126, "count": {{rows.Length}}, "type": "VEC3" }""")!;
            return (new Accessor(node, new[] { view }), buffer);
        }

        /// <summary>
        /// Builds a dense VEC4-float accessor (used for a primitive's base TANGENT attribute, which includes a handedness W).
        /// </summary>
        private static (Accessor accessor, TestBuffer buffer) Vec4Accessor(params float[][] rows)
        {
            byte[] bytes = new byte[rows.Length * 4 * sizeof(float)];
            System.Buffer.BlockCopy(rows.SelectMany(r => r).ToArray(), 0, bytes, 0, bytes.Length);
            var buffer = new TestBuffer(bytes, bytes.Length);
            var view = buffer.View(0, bytes.Length);
            JsonNode node = JsonNode.Parse($$"""{ "bufferView": 0, "componentType": 5126, "count": {{rows.Length}}, "type": "VEC4" }""")!;
            return (new Accessor(node, new[] { view }), buffer);
        }

        [Fact]
        public void ParsesPositionNormalAndTangentDeltas()
        {
            var (position, positionBuffer) = Vec3Accessor(new float[] { 1f, 0f, 0f });
            var (normal, normalBuffer) = Vec3Accessor(new float[] { 0f, 1f, 0f });
            // Per the glTF 2.0 spec, TANGENT on a morph target is a VEC3 (no handedness W), unlike a primitive's base TANGENT attribute.
            var (tangent, tangentBuffer) = Vec3Accessor(new float[] { 0f, 0f, 1f });
            using var _p = positionBuffer; using var _n = normalBuffer; using var _t = tangentBuffer;

            var accessors = new[] { position, normal, tangent };
            JsonNode targetNode = JsonNode.Parse("""{ "POSITION": 0, "NORMAL": 1, "TANGENT": 2 }""")!;

            var target = new MorphTarget(targetNode, accessors);

            Assert.Equal(3, target.Attributes.Count);
            Assert.Equal(new float[] { 1f, 0f, 0f }, (float[])target.Attributes[new VertexAttribute(PrimitiveAttributes.POSITION, null)][0]);
            Assert.Equal(new float[] { 0f, 1f, 0f }, (float[])target.Attributes[new VertexAttribute(PrimitiveAttributes.NORMAL, null)][0]);
            var tangentRow = (float[])target.Attributes[new VertexAttribute(PrimitiveAttributes.TANGENT, null)][0];
            Assert.Equal(3, tangentRow.Length);
            Assert.Equal(new float[] { 0f, 0f, 1f }, tangentRow);
            Assert.Null(target.TintColor);
        }

        [Fact]
        public void RejectsATargetAttributeThatIsNotPositionNormalOrTangent()
        {
            var (texcoord, buffer) = Vec3Accessor(new float[] { 0f, 0f, 0f });
            using var _ = buffer;

            JsonNode targetNode = JsonNode.Parse("""{ "TEXCOORD": 0 }""")!;

            Assert.Throws<InvalidOperationException>(() => new MorphTarget(targetNode, new[] { texcoord }));
        }

        [Fact]
        public void DefaultsTintColorAlphaToOneWhenOnlyRgbIsGiven()
        {
            JsonNode targetNode = JsonNode.Parse("""{ "extras": { "SUE_tintColor": [0.2, 0.4, 0.6] } }""")!;

            var target = new MorphTarget(targetNode, Array.Empty<Accessor>());

            Assert.Empty(target.Attributes);
            Assert.Equal(new float[] { 0.2f, 0.4f, 0.6f, 1f }, target.TintColor);
        }

        [Fact]
        public void ReadsAnExplicitTintColorAlpha()
        {
            JsonNode targetNode = JsonNode.Parse("""{ "extras": { "SUE_tintColor": [1.0, 0.0, 0.0, 0.5] } }""")!;

            var target = new MorphTarget(targetNode, Array.Empty<Accessor>());

            Assert.Equal(new float[] { 1f, 0f, 0f, 0.5f }, target.TintColor);
        }

        [Fact]
        public void LeavesTintColorNullWhenNoExtrasArePresent()
        {
            var (position, buffer) = Vec3Accessor(new float[] { 0f, 0f, 0f });
            using var _ = buffer;
            JsonNode targetNode = JsonNode.Parse("""{ "POSITION": 0 }""")!;

            var target = new MorphTarget(targetNode, new[] { position });

            Assert.Null(target.TintColor);
        }

        [Fact]
        public void RejectsATintColorArrayOfTheWrongLength()
        {
            JsonNode targetNode = JsonNode.Parse("""{ "extras": { "SUE_tintColor": [1.0, 0.0] } }""")!;

            Assert.Throws<InvalidOperationException>(() => new MorphTarget(targetNode, Array.Empty<Accessor>()));
        }

        [Fact]
        public void BasePrimitiveTangentIsFourComponentsWhileMorphTargetTangentIsThree()
        {
            var (baseTangent, baseBuffer) = Vec4Accessor(new float[] { 1f, 0f, 0f, -1f });
            using var _b = baseBuffer;
            var baseRow = (float[])Primitive.CastAsPrimitiveData(PrimitiveAttributes.TANGENT, AccessorComponentType.Float, baseTangent.Data)[0];
            Assert.Equal(4, baseRow.Length);
            Assert.Equal(new float[] { 1f, 0f, 0f, -1f }, baseRow);

            var (targetTangent, targetBuffer) = Vec3Accessor(new float[] { 1f, 0f, 0f });
            using var _t = targetBuffer;
            var targetRow = (float[])Primitive.CastAsPrimitiveData(PrimitiveAttributes.TANGENT, AccessorComponentType.Float, targetTangent.Data)[0];
            Assert.Equal(3, targetRow.Length);
            Assert.Equal(new float[] { 1f, 0f, 0f }, targetRow);
        }
    }
}
