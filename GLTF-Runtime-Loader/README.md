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

# Versioning and Publishing

This package (`ShakeUpGames.GLTF.Runtime`) has no MinVer/git-tag-based automation — its version is a plain, hand-maintained `<Version>` string in `GLTF Runtime Loader/GLTF Runtime Loader.csproj`. `ShakeUpGames.Common3D` (in this same repo, `ShakeUpGames.Common3D/ShakeUpGames.Common3D.csproj`) works the same way.

**As an agent, if your changes touch either project here, you are expected to bump and publish, not just commit source:**

1. **Bump `<Version>` to a number that has never been published before** — check what's already on the feed (`curl <feed>/registration/<package-id-lowercase>/index.json`, or `dotnet nuget locals global-packages --list` for what's cached locally) before picking one. Reusing an already-published version number is exactly what caused a real, hard-to-diagnose bug here: two different builds got published under the same `0.1.1`/`Common3D 1.0.0`-style tag at different times, and a consumer's NuGet cache silently kept using whichever one it happened to restore first, since NuGet treats a version number as immutable and never re-checks it. Pick semver deliberately (major/breaking, minor/addition, patch/fix) the same as any other package.
2. **Verify before publishing, not after**: delete `bin`/`obj` for a genuinely clean build, then actually load a real `.gltf` file through the library (not just unit tests — a bug in `Primitive`'s attribute-casting logic once passed every unit test while still crashing on real content) before packing.
3. **Publish**: `dotnet pack "GLTF Runtime Loader/GLTF Runtime Loader.csproj" -c Release` (and the `ShakeUpGames.Common3D` project the same way, if it changed too), then `dotnet nuget push <path-to-.nupkg> --source "Gitea Local"`.

**Gitea being unreachable is not a reason to stop or skip the rest of your task.** If the push fails because the local Gitea container isn't running, that's an infrastructure problem outside your control — finish and push your source commit as normal, note plainly in your report that publishing is still pending and why, and move on. Don't block other work waiting for it, and don't skip bumping `<Version>` just because the publish half can't complete right now.

# TODO

- Run a full code-cleanup pass: convert to file-scoped namespaces, resolve compiler/analyzer warnings, apply suggested `readonly`/nullable-annotation fixes, etc. Not urgent — a large, mechanical style pass across the whole library, best done as its own dedicated change rather than mixed into a feature PR.
