# GLTF Runtime Loader
A .NET 8.0 class library that uses the [glTF™ 2.0 specification](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html) to read glTF files into runtime objects with the buffers read into appropriate object types.

# How to Use
```csharp
using GLTFRuntime;


string binPath = "<path to directory containing bin files and/or image files>";
string filePath = "<path to gltf file>";
glTF library = new glTF(binPath, filePath);
foreach (var mesh in library.Meshes)
{
    Console.WriteLine(mesh.Name);
    foreach (var primitive in mesh.Primitives)
        Console.WriteLine($"\t{primitive}");
}

foreach (var material in library.Materials)
{
    Console.WriteLine(material.Name);
    Console.WriteLine($"\t{material.PBRMetallicRoughness.BaseColorTexture?.Texture.Name ?? "Unnamed Texture"}");
}
```

# Status

This reader implements most parts of the glTF™ 2.0 standard, as detailed at [https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html).

Extensions, extras, sparse storage, and morph targets are among the features not yet implemented.

Documentation comments are mostly copied from the specification with application-specific notes added, but there may be some discrepancies where copy-paste fatigue set in.

**Pull requests are welcome!**
