using GLTFRuntime;
using System.Text.Json.Nodes;

namespace GLTF_Runtime_Loader.Tests
{
    public class AccessorTests
    {
        private static byte[] PackFloats(params float[] values)
        {
            byte[] bytes = new byte[values.Length * sizeof(float)];
            System.Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static byte[] Concat(params byte[][] chunks)
        {
            int total = chunks.Sum(c => c.Length);
            byte[] result = new byte[total];
            int offset = 0;
            foreach (var chunk in chunks)
            {
                System.Buffer.BlockCopy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }
            return result;
        }

        private static byte[] PackUShorts(params ushort[] values)
        {
            byte[] bytes = new byte[values.Length * sizeof(ushort)];
            for (int i = 0; i < values.Length; i++)
            {
                var b = BitConverter.GetBytes(values[i]);
                bytes[i * 2] = b[0];
                bytes[i * 2 + 1] = b[1];
            }
            return bytes;
        }

        private static float[] Row(Accessor accessor, int index) =>
            accessor.Data[index].Select(v => (float)v).ToArray();

        [Fact]
        public void ReadsADensePositionAccessor()
        {
            byte[] data = PackFloats(0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f);
            using var buffer = new TestBuffer(data, data.Length);
            var view = buffer.View(0, data.Length);

            JsonNode accessorNode = JsonNode.Parse("""{ "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3" }""")!;
            var accessor = new Accessor(accessorNode, new[] { view });

            Assert.Equal(3, accessor.Count);
            Assert.Equal(new float[] { 0f, 0f, 0f }, Row(accessor, 0));
            Assert.Equal(new float[] { 1f, 0f, 0f }, Row(accessor, 1));
            Assert.Equal(new float[] { 0f, 1f, 0f }, Row(accessor, 2));
        }

        [Fact]
        public void HonorsAccessorByteOffsetWithinASharedBufferView()
        {
            // Two VEC3 float accessors packed back-to-back in one bufferView.
            byte[] first = PackFloats(1f, 1f, 1f);
            byte[] second = PackFloats(2f, 2f, 2f);
            byte[] data = Concat(first, second);
            using var buffer = new TestBuffer(data, data.Length);
            var view = buffer.View(0, data.Length);

            JsonNode firstNode = JsonNode.Parse("""{ "bufferView": 0, "byteOffset": 0, "componentType": 5126, "count": 1, "type": "VEC3" }""")!;
            JsonNode secondNode = JsonNode.Parse($$"""{ "bufferView": 0, "byteOffset": {{first.Length}}, "componentType": 5126, "count": 1, "type": "VEC3" }""")!;

            var firstAccessor = new Accessor(firstNode, new[] { view });
            var secondAccessor = new Accessor(secondNode, new[] { view });

            Assert.Equal(new float[] { 1f, 1f, 1f }, Row(firstAccessor, 0));
            Assert.Equal(new float[] { 2f, 2f, 2f }, Row(secondAccessor, 0));
        }

        [Fact]
        public void AppliesSparseOverridesOnTopOfADenseBufferView()
        {
            byte[] baseData = PackFloats(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
            byte[] sparseIndices = PackUShorts(1);
            byte[] sparseValues = PackFloats(9f, 9f, 9f);
            byte[] data = Concat(baseData, sparseIndices, sparseValues);
            using var buffer = new TestBuffer(data, data.Length);

            var baseView = buffer.View(0, baseData.Length);
            var indicesView = buffer.View(baseData.Length, sparseIndices.Length);
            var valuesView = buffer.View(baseData.Length + sparseIndices.Length, sparseValues.Length);

            JsonNode accessorNode = JsonNode.Parse("""
            {
              "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3",
              "sparse": {
                "count": 1,
                "indices": { "bufferView": 1, "componentType": 5123 },
                "values": { "bufferView": 2 }
              }
            }
            """)!;

            var accessor = new Accessor(accessorNode, new[] { baseView, indicesView, valuesView });

            Assert.Equal(new float[] { 0f, 0f, 0f }, Row(accessor, 0));
            Assert.Equal(new float[] { 9f, 9f, 9f }, Row(accessor, 1));
            Assert.Equal(new float[] { 0f, 0f, 0f }, Row(accessor, 2));
        }

        [Fact]
        public void SparseAccessorWithNoBufferViewIsZeroFilledExceptForOverrides()
        {
            byte[] sparseIndices = PackUShorts(2);
            byte[] sparseValues = PackFloats(5f, 6f, 7f);
            byte[] data = Concat(sparseIndices, sparseValues);
            using var buffer = new TestBuffer(data, data.Length);

            var indicesView = buffer.View(0, sparseIndices.Length);
            var valuesView = buffer.View(sparseIndices.Length, sparseValues.Length);

            JsonNode accessorNode = JsonNode.Parse("""
            {
              "componentType": 5126, "count": 3, "type": "VEC3",
              "sparse": {
                "count": 1,
                "indices": { "bufferView": 0, "componentType": 5123 },
                "values": { "bufferView": 1 }
              }
            }
            """)!;

            var accessor = new Accessor(accessorNode, new[] { indicesView, valuesView });

            Assert.Null(accessor.BufferView);
            Assert.Equal(new float[] { 0f, 0f, 0f }, Row(accessor, 0));
            Assert.Equal(new float[] { 0f, 0f, 0f }, Row(accessor, 1));
            Assert.Equal(new float[] { 5f, 6f, 7f }, Row(accessor, 2));
        }

        [Fact]
        public void RejectsASparseIndicesComponentTypeThatIsNotUnsigned()
        {
            byte[] sparseIndices = new byte[2]; // a signed short (componentType 5122) is not legal for sparse indices
            byte[] sparseValues = PackFloats(1f, 1f, 1f);
            byte[] data = Concat(sparseIndices, sparseValues);
            using var buffer = new TestBuffer(data, data.Length);

            var indicesView = buffer.View(0, sparseIndices.Length);
            var valuesView = buffer.View(sparseIndices.Length, sparseValues.Length);

            JsonNode accessorNode = JsonNode.Parse("""
            {
              "componentType": 5126, "count": 3, "type": "VEC3",
              "sparse": {
                "count": 1,
                "indices": { "bufferView": 0, "componentType": 5122 },
                "values": { "bufferView": 1 }
              }
            }
            """)!;

            Assert.Throws<InvalidOperationException>(() => new Accessor(accessorNode, new[] { indicesView, valuesView }));
        }

        [Fact]
        public void RejectsASparseOverrideIndexOutsideTheAccessorsCount()
        {
            byte[] sparseIndices = PackUShorts(5); // accessor only has 3 elements
            byte[] sparseValues = PackFloats(1f, 1f, 1f);
            byte[] data = Concat(sparseIndices, sparseValues);
            using var buffer = new TestBuffer(data, data.Length);

            var indicesView = buffer.View(0, sparseIndices.Length);
            var valuesView = buffer.View(sparseIndices.Length, sparseValues.Length);

            JsonNode accessorNode = JsonNode.Parse("""
            {
              "componentType": 5126, "count": 3, "type": "VEC3",
              "sparse": {
                "count": 1,
                "indices": { "bufferView": 0, "componentType": 5123 },
                "values": { "bufferView": 1 }
              }
            }
            """)!;

            Assert.Throws<InvalidOperationException>(() => new Accessor(accessorNode, new[] { indicesView, valuesView }));
        }
    }
}
