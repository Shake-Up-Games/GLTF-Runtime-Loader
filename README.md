# GLTF Runtime Loader
A .NET 8.0 class library that uses the glTF™ 2.0 specification to read glTF files into runtime objects with the buffers read into appropriate object types.

# How to Use
```csharp
using GLTFRuntime;


string binPath = "<path to directory containing bin files and/or image files>";
string filePath = "<path to gltf file>";
glTF myFile = new GLTFRuntime.glTF(binPath, filePath);
foreach (Mesh mesh in GLTFRuntime.Meshes)
{
    Console.WriteLine(mesh.Name);
}
```

# Status

This reader implements most parts of the glTF™ 2.0 standard, as detailed at [https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html).

Extensions, extras, sparse storage, and morph targets are among the features not yet implemented.

**Pull requests are welcome!**