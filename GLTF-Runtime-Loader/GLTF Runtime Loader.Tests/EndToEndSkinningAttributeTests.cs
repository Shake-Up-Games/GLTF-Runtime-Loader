using GLTFRuntime;
using ShakeUpGames.Common3D;

namespace GLTF_Runtime_Loader.Tests
{
    /// <summary>
    /// Loads a small, hand-built glTF file (JSON + a matching .bin buffer, generated on the fly rather than checked in)
    /// through the public <see cref="glTF"/> entry point, exercising the full pipeline for a skinned primitive:
    /// glTF -> Mesh -> Primitive's constructor -> <see cref="Primitive.CastAsPrimitiveData"/>, for a
    /// <c>JOINTS_0</c>/<c>WEIGHTS_0</c> attribute pair shaped exactly like a real Blender-exported skinned mesh
    /// (unsigned byte VEC4 JOINTS_0, normalized unsigned byte VEC4 WEIGHTS_0).
    /// <para><see cref="GLTF_Runtime_Loader.Tests.PrimitiveAttributeCastingTests"/> already covers
    /// <see cref="Primitive.CastAsPrimitiveData"/> directly at the unit level with hand-built <see cref="Accessor"/>
    /// instances. This test instead covers the surrounding wiring - that <see cref="Primitive"/>'s constructor
    /// actually looks up the right <see cref="Accessor"/> by attribute index and threads its real
    /// <see cref="Accessor.ComponentType"/> (parsed from JSON) into <see cref="Primitive.CastAsPrimitiveData"/> -
    /// end to end, the same way a real content file is loaded (see <see cref="GLTFRuntime.glTF"/>'s constructor).</para>
    /// </summary>
    public class EndToEndSkinningAttributeTests : IDisposable
    {
        private readonly string directory;

        public EndToEndSkinningAttributeTests()
        {
            directory = Path.Combine(Path.GetTempPath(), "gltf-runtime-loader-e2e-skin-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        public void Dispose() => Directory.Delete(directory, recursive: true);

        [Fact]
        public void LoadsAPrimitiveWithByteJointsAndNormalizedByteWeights()
        {
            using MemoryStream bin = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(bin);

            int WriteFloats(params float[] values)
            {
                int offset = (int)bin.Position;
                foreach (float v in values)
                    writer.Write(v);
                return offset;
            }

            int WriteBytes(params byte[] values)
            {
                int offset = (int)bin.Position;
                foreach (byte v in values)
                    writer.Write(v);
                return offset;
            }

            // A single triangle: base POSITION, JOINTS_0 (unsigned byte VEC4, unnormalized - literal joint indices),
            // and WEIGHTS_0 (unsigned byte VEC4, normalized - a raw 255 means 1.0), exactly the shapes a real
            // Blender-exported skinned mesh (e.g. this repo's Man.gltf fixture) actually declares for these two
            // attributes: componentType 5121 (UNSIGNED_BYTE), type VEC4.
            int positionOffset = WriteFloats(0f, 0f, 0f, /**/ 1f, 0f, 0f, /**/ 0f, 1f, 0f);
            int jointsOffset = WriteBytes(0, 1, 0, 0, /**/ 1, 0, 0, 0, /**/ 0, 0, 0, 0);
            int weightsOffset = WriteBytes(255, 0, 0, 0, /**/ 128, 127, 0, 0, /**/ 255, 0, 0, 0);

            writer.Flush();
            int totalLength = (int)bin.Position;
            File.WriteAllBytes(Path.Combine(directory, "test.bin"), bin.ToArray());

            string json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "uri": "test.bin", "byteLength": {{totalLength}} } ],
              "bufferViews": [
                { "buffer": 0, "byteOffset": {{positionOffset}}, "byteLength": 36 },
                { "buffer": 0, "byteOffset": {{jointsOffset}}, "byteLength": 12 },
                { "buffer": 0, "byteOffset": {{weightsOffset}}, "byteLength": 12 }
              ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3" },
                { "bufferView": 1, "componentType": 5121, "count": 3, "type": "VEC4" },
                { "bufferView": 2, "componentType": 5121, "normalized": true, "count": 3, "type": "VEC4" }
              ],
              "meshes": [
                {
                  "name": "SkinnedTriangle",
                  "primitives": [
                    {
                      "attributes": { "POSITION": 0, "JOINTS_0": 1, "WEIGHTS_0": 2 }
                    }
                  ]
                }
              ]
            }
            """;
            File.WriteAllText(Path.Combine(directory, "test.gltf"), json);

            glTF result = new glTF(directory, "test.gltf");

            Mesh mesh = result.Meshes["SkinnedTriangle"];
            Primitive primitive = mesh.Primitives[0];

            var jointsKey = new VertexAttribute(PrimitiveAttributes.JOINTS, 0);
            var weightsKey = new VertexAttribute(PrimitiveAttributes.WEIGHTS, 0);

            Assert.True(primitive.Attributes.ContainsKey(jointsKey));
            Assert.True(primitive.Attributes.ContainsKey(weightsKey));

            var joints = primitive.Attributes[jointsKey];
            // Byte JOINTS_0 must widen to ushort, per Primitive.CastAsPrimitiveData's documented contract, and must
            // reflect the accessor's own ComponentType (Byte here) rather than crashing or misreading it as Float.
            Assert.Equal(new ushort[] { 0, 1, 0, 0 }, (ushort[])joints[0]);
            Assert.Equal(new ushort[] { 1, 0, 0, 0 }, (ushort[])joints[1]);
            Assert.Equal(new ushort[] { 0, 0, 0, 0 }, (ushort[])joints[2]);

            var weights = primitive.Attributes[weightsKey];
            // Normalized byte WEIGHTS_0 must be divided down to [0, 1] floats.
            Assert.Equal(1f, ((float[])weights[0])[0], precision: 5);
            Assert.Equal(128f / 255f, ((float[])weights[1])[0], precision: 5);
            Assert.Equal(127f / 255f, ((float[])weights[1])[1], precision: 5);
            Assert.Equal(1f, ((float[])weights[2])[0], precision: 5);
        }
    }
}
