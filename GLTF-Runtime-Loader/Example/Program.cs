using GLTFRuntime;

namespace Example
{
    internal class Program
    {
        static void Main(string[] args)
        {
            glTF monkey = new glTF("", "monkey.gltf");
            foreach (var mesh in monkey.Meshes)
            {
                Console.WriteLine(mesh.Name);
                foreach (var primitive in mesh.Primitives)
                    Console.WriteLine($"\t{primitive}");
            }

            foreach (var material in monkey.Materials)
            {
                Console.WriteLine(material.Name);
                Console.WriteLine($"\t{material.PBRMetallicRoughness.BaseColorTexture?.Texture.Name ?? "Unnamed Texture"}");
            }
        }
    }
}
