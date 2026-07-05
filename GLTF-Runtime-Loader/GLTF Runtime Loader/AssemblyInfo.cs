using System.Runtime.CompilerServices;

// Grants the test project access to internal constructors (Accessor, BufferView, MorphTarget, Primitive, Mesh, ...)
// so tests can build small, precise JSON/buffer fixtures directly instead of only through the public glTF(binDirectory, fileName) entry point.
[assembly: InternalsVisibleTo("GLTF Runtime Loader.Tests")]
