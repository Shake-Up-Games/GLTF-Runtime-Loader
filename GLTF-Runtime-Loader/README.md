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

Morph targets (`primitive.targets`) and sparse accessor storage are now implemented, including a small, non-standard `extras.SUE_tintColor` convention on individual morph targets for consumers that want to blend a color alongside a target's geometry (see the XML docs on `MorphTarget.TintColor` for the exact format and a caveat about authoring it in Blender).

Beyond that one convention, general `extras` and `extensions` support (i.e. reading arbitrary/registered extension data anywhere else in the file) is still not implemented.

Documentation comments are mostly copied from the specification with application-specific notes added, but there may be some discrepancies where copy-paste fatigue set in.

**Pull requests are welcome!**

# TODO

- Run a full code-cleanup pass: convert to file-scoped namespaces, resolve compiler/analyzer warnings, apply suggested `readonly`/nullable-annotation fixes, etc. Not urgent — a large, mechanical style pass across the whole library, best done as its own dedicated change rather than mixed into a feature PR.
