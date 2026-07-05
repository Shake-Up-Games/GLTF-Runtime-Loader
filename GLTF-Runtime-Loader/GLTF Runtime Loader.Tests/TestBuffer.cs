using GLTFRuntime;
using System.Text.Json.Nodes;

namespace GLTF_Runtime_Loader.Tests
{
    /// <summary>
    /// Writes a byte array to a temp file and wraps it as a <see cref="GLTFRuntime.Buffer"/>, since
    /// <see cref="GLTFRuntime.Buffer"/> only ever reads its bytes from disk. Deletes the temp file on dispose.
    /// </summary>
    internal sealed class TestBuffer : IDisposable
    {
        public GLTFRuntime.Buffer Buffer { get; }

        private readonly string filePath;

        public TestBuffer(byte[] bytes, int byteLength)
        {
            string directory = Path.Combine(Path.GetTempPath(), "gltf-runtime-loader-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            filePath = Path.Combine(directory, "data.bin");
            File.WriteAllBytes(filePath, bytes);

            JsonNode bufferNode = JsonNode.Parse($$"""{ "uri": "data.bin", "byteLength": {{byteLength}} }""")!;
            Buffer = new GLTFRuntime.Buffer(bufferNode, directory);
        }

        public void Dispose()
        {
            var directory = Path.GetDirectoryName(filePath);
            if (directory != null && Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        /// <summary>
        /// Builds a <see cref="BufferView"/> over this buffer's bytes at the given offset/length.
        /// </summary>
        public BufferView View(int byteOffset, int byteLength)
        {
            JsonNode viewNode = JsonNode.Parse($$"""{ "buffer": 0, "byteOffset": {{byteOffset}}, "byteLength": {{byteLength}} }""")!;
            return new BufferView(viewNode, new[] { Buffer });
        }
    }
}
