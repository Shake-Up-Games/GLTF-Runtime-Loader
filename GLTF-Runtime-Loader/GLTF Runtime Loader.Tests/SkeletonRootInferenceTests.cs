using GLTFRuntime;

namespace GLTF_Runtime_Loader.Tests
{
    /// <summary>
    /// Regression test for a real bug found loading a real Blender-exported file (not a hand-built one):
    /// <see cref="Skin"/>'s inference of the skeleton root, when the file's own <c>skin.skeleton</c> is
    /// omitted, used to check whether a joint's *parent* had no explicit transform keys - a signal with no
    /// real connection to "is this the skeleton root." Any completely ordinary bone chain where a child
    /// bone's head sits exactly at its parent's local origin (ubiquitous - it's what Blender produces for
    /// a simple two-bone chain with no offset) gives more than one joint a parent with an identity
    /// transform, so the old check's <c>Single()</c> would throw "Sequence contains more than one matching
    /// element" on ordinary content that has nothing wrong with it.
    /// </summary>
    public class SkeletonRootInferenceTests : IDisposable
    {
        private readonly string directory;

        public SkeletonRootInferenceTests()
        {
            directory = Path.Combine(Path.GetTempPath(), "gltf-runtime-loader-skel-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        public void Dispose() => Directory.Delete(directory, recursive: true);

        [Fact]
        public void InfersTheCorrectRootWhenTwoJointsHaveAnIdentityTransformParent()
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

            int positionOffset = WriteFloats(0f, 0f, 0f, /**/ 1f, 0f, 0f, /**/ 0f, 1f, 0f);
            int normalOffset = WriteFloats(0f, 0f, 1f, /**/ 0f, 0f, 1f, /**/ 0f, 0f, 1f);
            int jointsOffset = WriteUShorts(0, 0, 0, 0, /**/ 0, 0, 0, 0, /**/ 0, 0, 0, 0);
            int weightsOffset = WriteFloats(1f, 0f, 0f, 0f, /**/ 1f, 0f, 0f, 0f, /**/ 1f, 0f, 0f, 0f);
            int indicesOffset = WriteUShorts(0, 1, 2);

            writer.Flush();
            int totalLength = (int)bin.Position;
            File.WriteAllBytes(Path.Combine(directory, "test.bin"), bin.ToArray());

            // Node 0 ("armature") and node 1 ("base") both have no translation/rotation/scale keys at all
            // (an identity local transform, exactly what a real Blender export produces when a bone's head
            // sits at its parent's local origin) - node 2 ("sway") is offset from its parent. Neither the
            // skin's own "skeleton" field is present, forcing inference. Under the old (buggy) heuristic,
            // both joint 0 ("base", parent = "armature", NoTransform) and joint 1 ("sway", parent = "base",
            // NoTransform) would match, throwing. The fix must resolve to exactly joint 0 ("base"), since
            // its parent ("armature") isn't itself one of this skin's joints, while joint 1's parent is.
            string json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "uri": "test.bin", "byteLength": {{totalLength}} } ],
              "bufferViews": [
                { "buffer": 0, "byteOffset": {{positionOffset}}, "byteLength": 36 },
                { "buffer": 0, "byteOffset": {{normalOffset}}, "byteLength": 36 },
                { "buffer": 0, "byteOffset": {{jointsOffset}}, "byteLength": 24 },
                { "buffer": 0, "byteOffset": {{weightsOffset}}, "byteLength": 48 },
                { "buffer": 0, "byteOffset": {{indicesOffset}}, "byteLength": 6 }
              ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3" },
                { "bufferView": 1, "componentType": 5126, "count": 3, "type": "VEC3" },
                { "bufferView": 2, "componentType": 5121, "count": 3, "type": "VEC4" },
                { "bufferView": 3, "componentType": 5126, "count": 3, "type": "VEC4" },
                { "bufferView": 4, "componentType": 5123, "count": 3, "type": "SCALAR" }
              ],
              "meshes": [
                {
                  "name": "Triangle",
                  "primitives": [
                    {
                      "attributes": { "POSITION": 0, "NORMAL": 1, "JOINTS_0": 2, "WEIGHTS_0": 3 },
                      "indices": 4
                    }
                  ]
                }
              ],
              "skins": [
                { "joints": [1, 2] }
              ],
              "nodes": [
                { "name": "armature", "children": [1] },
                { "name": "base", "children": [2] },
                { "name": "sway", "translation": [0.0, 0.5, 0.0] },
                { "name": "Triangle", "mesh": 0, "skin": 0 }
              ],
              "scenes": [ { "nodes": [0, 3] } ],
              "scene": 0
            }
            """;
            File.WriteAllText(Path.Combine(directory, "test.gltf"), json);

            glTF result = new glTF(directory, "test.gltf");

            Skin skin = result.Skins!.Single();
            Assert.NotNull(skin.Skeleton);
            Assert.Equal("base", skin.Skeleton!.Name);
        }
    }
}
