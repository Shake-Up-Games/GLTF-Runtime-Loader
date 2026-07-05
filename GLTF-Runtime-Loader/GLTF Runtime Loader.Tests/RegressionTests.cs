using GLTFRuntime;
using ShakeUpGames.Common3D;

namespace GLTF_Runtime_Loader.Tests
{
    /// <summary>
    /// Loads the pre-existing "monkey" fixture (which has no morph targets and no sparse accessors) through the
    /// public <see cref="glTF"/> entry point, to confirm that generalizing <see cref="Accessor"/>/<see cref="BufferView"/>
    /// to support accessor byteOffset and sparse storage did not change behavior for ordinary, dense, non-morph content.
    /// </summary>
    public class RegressionTests
    {
        private static string FixturesDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures");

        [Fact]
        public void StillLoadsTheExistingMonkeyFixtureWithoutMorphTargets()
        {
            glTF monkey = new glTF(FixturesDirectory, "monkey.gltf");

            Assert.NotEmpty(monkey.Meshes);
            Assert.NotEmpty(monkey.Materials);

            foreach (var mesh in monkey.Meshes.Values)
            {
                Assert.NotEmpty(mesh.Primitives);
                foreach (var primitive in mesh.Primitives)
                {
                    // None of this file's primitives define morph targets.
                    Assert.Null(primitive.Targets);
                    Assert.True(primitive.Attributes.ContainsKey(new VertexAttribute(PrimitiveAttributes.POSITION, null)));
                }
            }
        }
    }
}
