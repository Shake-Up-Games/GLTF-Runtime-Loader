using GLTFRuntime;
using ShakeUpGames.Common3D;
using System.Text.Json.Nodes;

namespace GLTF_Runtime_Loader.Tests
{
    /// <summary>
    /// Regression tests for a real bug found loading a real Blender-exported skinned mesh (not a hand-built one):
    /// <see cref="Primitive.CastAsPrimitiveData(PrimitiveAttributes, AccessorComponentType, System.Collections.ObjectModel.ReadOnlyCollection{System.Collections.ObjectModel.ReadOnlyCollection{object}})"/>
    /// used to hard-code a single element type per attribute semantic (e.g. always <see cref="byte"/> for JOINTS,
    /// always <see cref="float"/> for WEIGHTS) and unbox each accessor element directly into that type. Per the
    /// glTF 2.0 specification, JOINTS_n accessors are legally <see cref="AccessorComponentType.Byte"/> OR
    /// <see cref="AccessorComponentType.UShort"/>, and WEIGHTS_n (and TEXCOORD_n) accessors are legally
    /// <see cref="AccessorComponentType.Float"/>, OR a *normalized* <see cref="AccessorComponentType.Byte"/>/<see
    /// cref="AccessorComponentType.UShort"/> - so the hard-coded assumption crashed with an <see
    /// cref="InvalidCastException"/> ("Unable to cast object of type 'System.Byte' to type 'System.Single'") the
    /// moment a real file used anything other than the one type each attribute happened to hard-code, which is
    /// exactly what happened with a real Blender export whose WEIGHTS_0 accessor used a normalized unsigned byte.
    /// </summary>
    public class PrimitiveAttributeCastingTests
    {
        /// <summary>
        /// Builds a dense VEC4 accessor of raw byte values, backed by its own temp buffer.
        /// </summary>
        private static (Accessor accessor, TestBuffer buffer) Vec4ByteAccessor(bool normalized, params byte[][] rows)
        {
            byte[] bytes = rows.SelectMany(r => r).ToArray();
            var buffer = new TestBuffer(bytes, bytes.Length);
            var view = buffer.View(0, bytes.Length);
            string normalizedJson = normalized ? ""","normalized": true""" : "";
            JsonNode node = JsonNode.Parse($$"""{ "bufferView": 0, "componentType": 5121{{normalizedJson}}, "count": {{rows.Length}}, "type": "VEC4" }""")!;
            return (new Accessor(node, new[] { view }), buffer);
        }

        /// <summary>
        /// Builds a dense VEC4 accessor of raw ushort values, backed by its own temp buffer.
        /// </summary>
        private static (Accessor accessor, TestBuffer buffer) Vec4UShortAccessor(bool normalized, params ushort[][] rows)
        {
            byte[] bytes = new byte[rows.Length * 4 * sizeof(ushort)];
            int offset = 0;
            foreach (var row in rows)
            {
                foreach (var value in row)
                {
                    var b = BitConverter.GetBytes(value);
                    bytes[offset] = b[0];
                    bytes[offset + 1] = b[1];
                    offset += 2;
                }
            }
            var buffer = new TestBuffer(bytes, bytes.Length);
            var view = buffer.View(0, bytes.Length);
            string normalizedJson = normalized ? ""","normalized": true""" : "";
            JsonNode node = JsonNode.Parse($$"""{ "bufferView": 0, "componentType": 5123{{normalizedJson}}, "count": {{rows.Length}}, "type": "VEC4" }""")!;
            return (new Accessor(node, new[] { view }), buffer);
        }

        /// <summary>
        /// Builds a dense VEC4-float accessor, backed by its own temp buffer.
        /// </summary>
        private static (Accessor accessor, TestBuffer buffer) Vec4FloatAccessor(params float[][] rows)
        {
            byte[] bytes = new byte[rows.Length * 4 * sizeof(float)];
            System.Buffer.BlockCopy(rows.SelectMany(r => r).ToArray(), 0, bytes, 0, bytes.Length);
            var buffer = new TestBuffer(bytes, bytes.Length);
            var view = buffer.View(0, bytes.Length);
            JsonNode node = JsonNode.Parse($$"""{ "bufferView": 0, "componentType": 5126, "count": {{rows.Length}}, "type": "VEC4" }""")!;
            return (new Accessor(node, new[] { view }), buffer);
        }

        [Fact]
        public void CastsJointsFromAnUnsignedByteAccessorToUShort()
        {
            var (accessor, buffer) = Vec4ByteAccessor(normalized: false, new byte[] { 3, 1, 0, 0 });
            using var _ = buffer;

            var result = Primitive.CastAsPrimitiveData(PrimitiveAttributes.JOINTS, accessor.ComponentType, accessor.Data);

            var row = (ushort[])result[0];
            Assert.Equal(new ushort[] { 3, 1, 0, 0 }, row);
        }

        [Fact]
        public void CastsJointsFromAnUnsignedShortAccessorToUShort()
        {
            // A real rig with more than 255 joints needs UShort JOINTS - this is legal per the glTF 2.0 spec and used
            // to throw InvalidCastException, since the old code only ever unboxed JOINTS elements as byte.
            var (accessor, buffer) = Vec4UShortAccessor(normalized: false, new ushort[] { 300, 12, 0, 0 });
            using var _ = buffer;

            var result = Primitive.CastAsPrimitiveData(PrimitiveAttributes.JOINTS, accessor.ComponentType, accessor.Data);

            var row = (ushort[])result[0];
            Assert.Equal(new ushort[] { 300, 12, 0, 0 }, row);
        }

        [Fact]
        public void CastsWeightsFromANormalizedUnsignedByteAccessorToFloat()
        {
            // This is the exact real-world case that crashed: a Blender-exported skinned mesh's WEIGHTS_0 accessor
            // using a normalized unsigned byte component type, which the old hard-coded castAsArrays<float> could
            // not unbox (it assumed every WEIGHTS accessor was already boxed as float).
            var (accessor, buffer) = Vec4ByteAccessor(normalized: true, new byte[] { 255, 128, 0, 0 });
            using var _ = buffer;

            var result = Primitive.CastAsPrimitiveData(PrimitiveAttributes.WEIGHTS, accessor.ComponentType, accessor.Data);

            var row = (float[])result[0];
            Assert.Equal(1f, row[0], precision: 5);
            Assert.Equal(128f / 255f, row[1], precision: 5);
            Assert.Equal(0f, row[2], precision: 5);
            Assert.Equal(0f, row[3], precision: 5);
        }

        [Fact]
        public void CastsWeightsFromANormalizedUnsignedShortAccessorToFloat()
        {
            var (accessor, buffer) = Vec4UShortAccessor(normalized: true, new ushort[] { 65535, 32768, 0, 0 });
            using var _ = buffer;

            var result = Primitive.CastAsPrimitiveData(PrimitiveAttributes.WEIGHTS, accessor.ComponentType, accessor.Data);

            var row = (float[])result[0];
            Assert.Equal(1f, row[0], precision: 5);
            Assert.Equal(32768f / 65535f, row[1], precision: 5);
            Assert.Equal(0f, row[2], precision: 5);
            Assert.Equal(0f, row[3], precision: 5);
        }

        [Fact]
        public void StillCastsWeightsFromAFloatAccessorUnchanged()
        {
            // The pre-existing, most common case (and the one path the old code got right) must keep working
            // exactly as before: float WEIGHTS data passed straight through, unscaled.
            var (accessor, buffer) = Vec4FloatAccessor(new float[] { 1f, 0f, 0f, 0f });
            using var _ = buffer;

            var result = Primitive.CastAsPrimitiveData(PrimitiveAttributes.WEIGHTS, accessor.ComponentType, accessor.Data);

            var row = (float[])result[0];
            Assert.Equal(new float[] { 1f, 0f, 0f, 0f }, row);
        }

        [Fact]
        public void StillCastsJointsFromAByteAccessorEvenThoughOutputIsNowUShort()
        {
            // Widening the unified output type to ushort (see CastAsPrimitiveData's remarks) must not regress the
            // pre-existing, most common case of byte JOINTS data - values must still come through correctly, just
            // boxed as ushort instead of byte now.
            var (accessor, buffer) = Vec4ByteAccessor(normalized: false, new byte[] { 0, 1, 2, 3 });
            using var _ = buffer;

            var result = Primitive.CastAsPrimitiveData(PrimitiveAttributes.JOINTS, accessor.ComponentType, accessor.Data);

            var row = (ushort[])result[0];
            Assert.Equal(new ushort[] { 0, 1, 2, 3 }, row);
        }
    }
}
