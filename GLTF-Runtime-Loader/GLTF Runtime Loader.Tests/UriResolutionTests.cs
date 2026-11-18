using System.Text;
using GLTFRuntime;
using ShakeUpGames.Common3D;

namespace GLTF_Runtime_Loader.Tests
{
    /// <summary>
    /// Covers how a buffer or image <c>uri</c> becomes bytes (glTF 2.0 section 2.8, "URIs"): relative paths are
    /// percent-decoded (with the literal name as a fallback unless <see cref="GltfUriDecoding.Literal"/> is chosen)
    /// and <c>data:</c> URIs are decoded in memory. Every test writes a hand-built glTF file and its resources into a
    /// temporary folder and loads it through the public <see cref="glTF"/> entry point.
    /// </summary>
    public class UriResolutionTests : IDisposable
    {
        private readonly string directory;

        public UriResolutionTests()
        {
            directory = Path.Combine(Path.GetTempPath(), "gltf-runtime-loader-uri-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        public void Dispose() => Directory.Delete(directory, recursive: true);

        private static readonly float[] TrianglePositions = { 0f, 0f, 0f, /**/ 1f, 0f, 0f, /**/ 0f, 1f, 0f };

        private static byte[] TriangleBytes()
        {
            using MemoryStream bin = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(bin);
            foreach (float v in TrianglePositions)
                writer.Write(v);
            writer.Flush();
            return bin.ToArray();
        }

        private static string Quote(string value) => System.Text.Json.JsonSerializer.Serialize(value);

        /// <summary>Writes a glTF file with one image (given uri) and no geometry; returns the loaded asset.</summary>
        private glTF LoadImage(string imageUri, GltfLoadOptions? options = null)
        {
            string json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [],
              "bufferViews": [],
              "accessors": [],
              "images": [ { "uri": {{Quote(imageUri)}} } ]
            }
            """;
            File.WriteAllText(Path.Combine(directory, "test.gltf"), json);
            return options == null ? new glTF(directory, "test.gltf") : new glTF(directory, "test.gltf", options);
        }

        /// <summary>Writes a glTF file whose only buffer has the given uri and holds one triangle of positions.</summary>
        private glTF LoadTriangleBuffer(string bufferUri, GltfLoadOptions? options = null)
        {
            string json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "uri": {{Quote(bufferUri)}}, "byteLength": 36 } ],
              "bufferViews": [ { "buffer": 0, "byteOffset": 0, "byteLength": 36 } ],
              "accessors": [ { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3" } ],
              "meshes": [ { "name": "T", "primitives": [ { "attributes": { "POSITION": 0 } } ] } ]
            }
            """;
            File.WriteAllText(Path.Combine(directory, "test.gltf"), json);
            return options == null ? new glTF(directory, "test.gltf") : new glTF(directory, "test.gltf", options);
        }

        private static byte[] ImageBytes(glTF gltf) => gltf.Images![0].Data!.ToArray();

        private static void AssertTrianglePositions(glTF gltf)
        {
            var positions = gltf.Meshes["T"].Primitives[0].Attributes[new VertexAttribute(PrimitiveAttributes.POSITION, null)];
            Assert.Equal(3, positions.Count);
            Assert.Equal(new float[] { 0f, 0f, 0f }, (float[])positions[0]);
            Assert.Equal(new float[] { 1f, 0f, 0f }, (float[])positions[1]);
            Assert.Equal(new float[] { 0f, 1f, 0f }, (float[])positions[2]);
        }

        [Fact]
        public void ImageUriWithPercent20LoadsTheFileWithASpace()
        {
            byte[] expected = { 1, 2, 3 };
            File.WriteAllBytes(Path.Combine(directory, "my tex.png"), expected);

            Assert.Equal(expected, ImageBytes(LoadImage("my%20tex.png")));
        }

        [Fact]
        public void BufferUriWithPercent20LoadsTheFileWithASpace()
        {
            File.WriteAllBytes(Path.Combine(directory, "my data.bin"), TriangleBytes());

            AssertTrianglePositions(LoadTriangleBuffer("my%20data.bin"));
        }

        [Fact]
        public void NonAsciiUrisLoadWhetherPercentEncodedOrWrittenAsIs()
        {
            byte[] expected = { 9, 8, 7 };
            File.WriteAllBytes(Path.Combine(directory, "café.png"), expected);

            Assert.Equal(expected, ImageBytes(LoadImage("caf%C3%A9.png")));
            Assert.Equal(expected, ImageBytes(LoadImage("café.png")));
        }

        [Fact]
        public void EncodedPercentSignLoadsTheFileWithAPercentSign()
        {
            byte[] expected = { 4, 5, 6 };
            File.WriteAllBytes(Path.Combine(directory, "100%.png"), expected);

            Assert.Equal(expected, ImageBytes(LoadImage("100%25.png")));
        }

        [Fact]
        public void LiteralNameStillLoadsWhenNoDecodedFileExists()
        {
            byte[] expected = { 1, 1, 1 };
            File.WriteAllBytes(Path.Combine(directory, "a%20b.png"), expected);

            Assert.Equal(expected, ImageBytes(LoadImage("a%20b.png")));
        }

        [Fact]
        public void DecodedFileWinsWhenBothTheDecodedAndTheLiteralFileExist()
        {
            byte[] decoded = { 1, 2, 3 };
            byte[] literal = { 4, 5, 6 };
            File.WriteAllBytes(Path.Combine(directory, "a b.png"), decoded);
            File.WriteAllBytes(Path.Combine(directory, "a%20b.png"), literal);

            Assert.Equal(decoded, ImageBytes(LoadImage("a%20b.png")));
        }

        [Fact]
        public void LiteralModeReadsOnlyTheLiteralName()
        {
            byte[] decoded = { 1, 2, 3 };
            byte[] literal = { 4, 5, 6 };
            var options = new GltfLoadOptions { UriDecoding = GltfUriDecoding.Literal };

            File.WriteAllBytes(Path.Combine(directory, "a b.png"), decoded);
            Assert.Throws<FileNotFoundException>(() => LoadImage("a%20b.png", options));

            File.WriteAllBytes(Path.Combine(directory, "a%20b.png"), literal);
            Assert.Equal(literal, ImageBytes(LoadImage("a%20b.png", options)));
        }

        [Fact]
        public void Base64DataUriBufferDecodesThroughTheFullPipeline()
        {
            string uri = "data:application/octet-stream;base64," + Convert.ToBase64String(TriangleBytes());

            AssertTrianglePositions(LoadTriangleBuffer(uri));
        }

        [Fact]
        public void Base64DataUriImageHasTheDecodedBytesAndMediaType()
        {
            byte[] expected = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

            glTF gltf = LoadImage("data:image/png;base64," + Convert.ToBase64String(expected));

            Assert.Equal(expected, ImageBytes(gltf));
            Assert.Equal("image/png", gltf.Images![0].MIMEType);
        }

        [Fact]
        public void PercentEncodedDataUriDecodesToTheRawOctets()
        {
            glTF gltf = LoadImage("data:text/plain,hello%20world");

            Assert.Equal(Encoding.ASCII.GetBytes("hello world"), ImageBytes(gltf));
            Assert.Equal("text/plain", gltf.Images![0].MIMEType);
        }

        [Fact]
        public void MalformedDataUrisThrowWithAMessageNamingTheProblem()
        {
            var badBase64 = Assert.Throws<FormatException>(() => LoadImage("data:image/png;base64,@@@not-base64@@@"));
            Assert.Contains("base64", badBase64.Message, StringComparison.OrdinalIgnoreCase);

            var noComma = Assert.Throws<InvalidOperationException>(() => LoadImage("data:image/png;base64"));
            Assert.Contains("comma", noComma.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MissingFileThrowsAndListsTheCandidatePathsTried()
        {
            var ex = Assert.Throws<FileNotFoundException>(() => LoadImage("no%20such.png"));

            Assert.Contains("no%20such.png", ex.Message, StringComparison.Ordinal);
            Assert.Contains(Path.Combine(directory, "no such.png"), ex.Message, StringComparison.Ordinal);
            Assert.Contains(Path.Combine(directory, "no%20such.png"), ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void RenamedCopyOfTheMonkeyFixtureLoadsThroughAnEscapedUri()
        {
            string fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures");
            File.Copy(Path.Combine(fixtures, "monkey.bin"), Path.Combine(directory, "my monkey.bin"));
            File.Copy(Path.Combine(fixtures, "bake_result.png"), Path.Combine(directory, "bake_result.png"));

            string original = File.ReadAllText(Path.Combine(fixtures, "monkey.gltf"));
            Assert.Contains("monkey.bin", original, StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(directory, "renamed.gltf"), original.Replace("monkey.bin", "my%20monkey.bin", StringComparison.Ordinal));

            glTF baseline = new glTF(fixtures, "monkey.gltf");
            glTF renamed = new glTF(directory, "renamed.gltf");

            Assert.Equal(baseline.Meshes.Keys, renamed.Meshes.Keys);
            foreach (var name in baseline.Meshes.Keys)
            {
                var expected = baseline.Meshes[name].Primitives[0].Attributes[new VertexAttribute(PrimitiveAttributes.POSITION, null)];
                var actual = renamed.Meshes[name].Primitives[0].Attributes[new VertexAttribute(PrimitiveAttributes.POSITION, null)];
                Assert.Equal(expected.Count, actual.Count);
                Assert.Equal((float[])expected[0], (float[])actual[0]);
                Assert.Equal((float[])expected[^1], (float[])actual[^1]);
            }
        }
    }
}
