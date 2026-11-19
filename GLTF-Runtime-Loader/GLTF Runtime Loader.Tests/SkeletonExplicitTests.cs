using GLTFRuntime;

namespace GLTF_Runtime_Loader.Tests
{
    /// <summary>
    /// Tests for an explicit <c>skin.skeleton</c>. glTF 2.0 section 5.28.2 (skin.skeleton): "The index of the
    /// node used as a skeleton root. The node MUST be the closest common root of the joints hierarchy or a
    /// direct or indirect parent node of the closest common root." The index is into the file's
    /// <c>nodes</c> array, not into <c>skin.joints</c>.
    /// </summary>
    public class SkeletonExplicitTests : IDisposable
    {
        private readonly string directory;

        public SkeletonExplicitTests()
        {
            directory = Path.Combine(Path.GetTempPath(), "gltf-runtime-loader-skel-explicit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        public void Dispose() => Directory.Delete(directory, recursive: true);

        // Hierarchy: 0 "armature" -> 1 "hip" -> 2 "spine" -> 3 "head". Joints are listed head first, so no
        // node index equals its position in the joints array except by accident.
        private Skin LoadSkin(int skeletonNodeIndex)
        {
            string json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [], "bufferViews": [], "accessors": [],
              "skins": [ { "joints": [3, 2, 1], "skeleton": {{skeletonNodeIndex}} } ],
              "nodes": [
                { "name": "armature", "children": [1] },
                { "name": "hip", "children": [2] },
                { "name": "spine", "children": [3] },
                { "name": "head" }
              ],
              "scenes": [ { "nodes": [0] } ],
              "scene": 0
            }
            """;
            File.WriteAllText(Path.Combine(directory, "test.gltf"), json);
            return new glTF(directory, "test.gltf").Skins!.Single();
        }

        [Fact]
        public void ExplicitSkeletonIsANodeIndexNotAJointsIndex()
        {
            // "hip" is node 1 but sits at joints[2]; the old reading returned joints[1], which is "spine".
            Skin skin = LoadSkin(skeletonNodeIndex: 1);
            Assert.NotNull(skin.Skeleton);
            Assert.Equal("hip", skin.Skeleton!.Name);
            Assert.Equal(1, skin.Skeleton.Index);
        }

        [Fact]
        public void ExplicitSkeletonMayBeAParentOfTheJointsThatIsNotAJointItself()
        {
            // The spec allows a direct or indirect parent of the closest common root. "armature" is node 0
            // and is not in joints; the old reading returned joints[0], which is "head".
            Skin skin = LoadSkin(skeletonNodeIndex: 0);
            Assert.NotNull(skin.Skeleton);
            Assert.Equal("armature", skin.Skeleton!.Name);
            Assert.DoesNotContain(skin.Skeleton, skin.Joints);
        }

        [Fact]
        public void ExplicitSkeletonPointsAtTheSameNodeObjectAsTheNodesList()
        {
            string json = """
            {
              "asset": { "version": "2.0" },
              "buffers": [], "bufferViews": [], "accessors": [],
              "skins": [ { "joints": [2, 1], "skeleton": 1 } ],
              "nodes": [ { "name": "a" }, { "name": "b", "children": [2] }, { "name": "c" } ]
            }
            """;
            File.WriteAllText(Path.Combine(directory, "test.gltf"), json);
            glTF result = new glTF(directory, "test.gltf");
            Assert.Same(result.Nodes[1], result.Skins!.Single().Skeleton);
        }
    }
}
