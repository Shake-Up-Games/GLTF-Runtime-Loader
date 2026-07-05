using GLTFRuntime;
using ShakeUpGames.Common3D;

namespace GLTF_Runtime_Loader.Tests
{
    /// <summary>
    /// Loads a small, hand-built glTF file (JSON + a matching .bin buffer, generated on the fly rather than checked in,
    /// so every byte offset is computed rather than hand-transcribed) through the public <see cref="glTF"/> entry point,
    /// exercising the whole pipeline: Mesh -> Primitive -> targets -> MorphTarget, including one dense morph target and
    /// one sparse morph target (the layout Blender's exporter actually produces for shape keys), plus the SUE_tintColor
    /// extras convention.
    /// </summary>
    public class EndToEndMorphTargetTests : IDisposable
    {
        private readonly string directory;

        public EndToEndMorphTargetTests()
        {
            directory = Path.Combine(Path.GetTempPath(), "gltf-runtime-loader-e2e-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        public void Dispose() => Directory.Delete(directory, recursive: true);

        [Fact]
        public void LoadsAPrimitiveWithADenseAndASparseMorphTarget()
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

            int WriteUShorts(params ushort[] values)
            {
                int offset = (int)bin.Position;
                foreach (ushort v in values)
                    writer.Write(v);
                return offset;
            }

            // A single triangle: base POSITION/NORMAL, an index buffer, and two morph targets.
            int positionOffset = WriteFloats(0f, 0f, 0f, /**/ 1f, 0f, 0f, /**/ 0f, 1f, 0f);
            int normalOffset = WriteFloats(0f, 0f, 1f, /**/ 0f, 0f, 1f, /**/ 0f, 0f, 1f);
            int indicesOffset = WriteUShorts(0, 1, 2);
            // Target 0: dense POSITION delta, uniformly lifting every vertex along +Z.
            int denseTargetOffset = WriteFloats(0f, 0f, 1f, /**/ 0f, 0f, 1f, /**/ 0f, 0f, 1f);
            // Target 1: sparse POSITION delta, only moving vertex index 2.
            int sparseIndicesOffset = WriteUShorts(2);
            int sparseValuesOffset = WriteFloats(2f, 0f, 0f);

            writer.Flush();
            int totalLength = (int)bin.Position;
            File.WriteAllBytes(Path.Combine(directory, "test.bin"), bin.ToArray());

            string json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "uri": "test.bin", "byteLength": {{totalLength}} } ],
              "bufferViews": [
                { "buffer": 0, "byteOffset": {{positionOffset}}, "byteLength": 36 },
                { "buffer": 0, "byteOffset": {{normalOffset}}, "byteLength": 36 },
                { "buffer": 0, "byteOffset": {{indicesOffset}}, "byteLength": 6 },
                { "buffer": 0, "byteOffset": {{denseTargetOffset}}, "byteLength": 36 },
                { "buffer": 0, "byteOffset": {{sparseIndicesOffset}}, "byteLength": 2 },
                { "buffer": 0, "byteOffset": {{sparseValuesOffset}}, "byteLength": 12 }
              ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3" },
                { "bufferView": 1, "componentType": 5126, "count": 3, "type": "VEC3" },
                { "bufferView": 2, "componentType": 5123, "count": 3, "type": "SCALAR" },
                { "bufferView": 3, "componentType": 5126, "count": 3, "type": "VEC3" },
                {
                  "componentType": 5126, "count": 3, "type": "VEC3",
                  "sparse": {
                    "count": 1,
                    "indices": { "bufferView": 4, "componentType": 5123 },
                    "values": { "bufferView": 5 }
                  }
                }
              ],
              "meshes": [
                {
                  "name": "Triangle",
                  "weights": [0.0, 0.0],
                  "primitives": [
                    {
                      "attributes": { "POSITION": 0, "NORMAL": 1 },
                      "indices": 2,
                      "targets": [
                        { "POSITION": 3, "extras": { "SUE_tintColor": [0.0, 0.0, 1.0] } },
                        { "POSITION": 4, "extras": { "SUE_tintColor": [1.0, 0.0, 0.0, 0.5] } }
                      ]
                    }
                  ]
                }
              ]
            }
            """;
            File.WriteAllText(Path.Combine(directory, "test.gltf"), json);

            glTF result = new glTF(directory, "test.gltf");

            Mesh mesh = result.Meshes["Triangle"];
            Assert.Equal(new float[] { 0f, 0f }, mesh.Weights);

            Primitive primitive = mesh.Primitives[0];
            Assert.NotNull(primitive.Targets);
            Assert.Equal(2, primitive.Targets!.Count);
            // Mesh.Weights and Primitive.Targets must line up one-to-one.
            Assert.Equal(mesh.Weights!.Length, primitive.Targets.Count);

            var positionKey = new VertexAttribute(PrimitiveAttributes.POSITION, null);

            MorphTarget dense = primitive.Targets[0];
            var densePositions = dense.Attributes[positionKey];
            Assert.Equal(new float[] { 0f, 0f, 1f }, (float[])densePositions[0]);
            Assert.Equal(new float[] { 0f, 0f, 1f }, (float[])densePositions[1]);
            Assert.Equal(new float[] { 0f, 0f, 1f }, (float[])densePositions[2]);
            Assert.Equal(new float[] { 0f, 0f, 1f, 1f }, dense.TintColor); // RGB -> alpha defaults to 1

            MorphTarget sparse = primitive.Targets[1];
            var sparsePositions = sparse.Attributes[positionKey];
            Assert.Equal(new float[] { 0f, 0f, 0f }, (float[])sparsePositions[0]); // untouched by the sparse override
            Assert.Equal(new float[] { 0f, 0f, 0f }, (float[])sparsePositions[1]); // untouched by the sparse override
            Assert.Equal(new float[] { 2f, 0f, 0f }, (float[])sparsePositions[2]); // the one overridden vertex
            Assert.Equal(new float[] { 1f, 0f, 0f, 0.5f }, sparse.TintColor);
        }
    }
}
