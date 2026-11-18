using GLTFRuntime;

namespace GLTF_Runtime_Loader.Tests;

public class MaterialDefaultsTests
{
    [Theory]
    [InlineData("", 1f, 1f, 1f, 1f)]
    [InlineData(", \"pbrMetallicRoughness\": {}", 1f, 1f, 1f, 1f)]
    [InlineData(", \"pbrMetallicRoughness\": {\"baseColorFactor\":[0.2,0.4,0.6,0.8]}", 0.2f, 0.4f, 0.6f, 0.8f)]
    [InlineData(", \"pbrMetallicRoughness\": {\"baseColorFactor\":[0,0,0,0]}", 0f, 0f, 0f, 0f)]
    public void LoadsBaseColorFactorWithSpecificationDefaults(string pbr, float r, float g, float b, float a)
    {
        string directory = Path.Combine(Path.GetTempPath(), "gltf-material-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string json = $$"""
                {
                  "asset": {"version":"2.0"},
                  "buffers": [], "bufferViews": [], "accessors": [],
                  "materials": [{"name":"test"{{pbr}}}]
                }
                """;
            File.WriteAllText(Path.Combine(directory, "material.gltf"), json);
            var model = new glTF(directory, "material.gltf");
            Assert.Equal(new[] { r, g, b, a }, model.Materials[0].PBRMetallicRoughness.BaseColorFactor);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
