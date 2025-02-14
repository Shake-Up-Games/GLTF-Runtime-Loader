using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    internal static class Extensions
    {
        public static int GetElementIndex(this JsonNode node)
        {
            return node?.Parent?.AsArray()!.IndexOf(node) ?? -1;
        }
    }
}
