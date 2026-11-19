# GLTF Runtime Loader

**Indexes:** `GLTFLOADER-01`, `GLTFLOADER-02`, `GLTFLOADER-03`, `GLTFLOADER-04`, `GLTFLOADER-05`, `GLTFLOADER-06`, `GLTFLOADER-07`

This is the behavior contract for the `GLTF Runtime Loader` package (published as
`ShakeUpGames.GLTF.Runtime`) — a .NET class library that reads a glTF™ 2.0 file and its associated
`.bin`/image files into plain runtime objects, with buffer data already decoded into typed arrays.
It states what behavior is guaranteed, indexed by REQ-ID, and how that guarantee is enforced by
tests. It does not replace this repository's own `README.md` (one level up, at
`GLTF-Runtime-Loader/README.md`), which answers "what does this package provide, how do you use
it"; this file answers "what behavior is contractually expected and how is it checked."

**Correction to `05-RESEARCH.md`:** that document lists this package among those with no test
project. That is wrong — the sibling `GLTF Runtime Loader.Tests/` project (in this same repository,
just not under `KenneyJam`'s `TestProjects/` tree, which is why the engine-repo inventory missed
it) exists and has real coverage. The claims-to-tests table below cites only classes and methods
read directly from that project.

Parsing here stops at typed, in-memory glTF objects. Turning that data into GPU-ready models,
skins, and the instanced-rendering/VTF-baking pipeline is `Engine-Packages/glTF/SPEC.md`'s
(`GLTF-NN`) concern, in the separate engine repository — cross-referenced below where relevant,
never duplicated here.

## Loading entry point and optional top-level collections

**Rule.** `new glTF(binDirectory, fileName)` (`glTF.cs`) parses the file at
`Path.Combine(binDirectory, fileName)` as JSON and populates every top-level collection in one
constructor call — `Asset` (required by the glTF spec; constructed unconditionally), `Cameras`,
`Nodes`, `Scenes`, `Scene`, `Images`, `Samplers`, `Textures`, `Materials`, `Meshes` (a
`ReadOnlyDictionary<string, Mesh>` keyed by mesh name), `MeshList` (a `ReadOnlyCollection<Mesh>` in
file order, letting a mesh be looked up by `Node.MeshIndex` even when two meshes share a name),
`Skins`, and `Animations`. Every one of these collections is non-null even when the corresponding
JSON array is absent from the file — an absent `"nodes"`/`"cameras"`/`"meshes"`/etc. array produces
an *empty* `ReadOnlyCollection`/`ReadOnlyDictionary`, never a null reference, so a caller never
needs a null check before enumerating. `Node` parent/child links (`Node.Children`,
`Node.Parent`) are wired after every node object already exists, by walking each node's
`"children"` index array a second time — so a child's `Parent` is always set correctly regardless
of whether the child or the parent appears first in the file's `"nodes"` array.

**Consequence of getting it wrong, in each direction:** Returning `null` instead of an empty
collection for an absent JSON array would push a null-check burden onto every consumer that
otherwise assumes "the file just has zero of these" is the same shape as "the property was
omitted." Wiring `Node.Parent`/`Node.Children` in a single forward pass over the node array
(instead of a full first pass to construct every `Node`, then a second pass to link them) would
silently miss forward references — a node whose children are declared before it appears in the
array — since `nodes[childIndex]` in the linking loop depends on every node object already
existing.

**Generalization.** Any future top-level glTF array this package adds should follow the same
"non-null empty collection when absent, never a null reference" contract already established for
every existing collection, and any future cross-referencing structure (parent/child links, or
anything else that indexes into an array being built) should build the full flat collection first,
then link in a second pass — the same two-pass shape `Nodes` already uses.

## Accessor/BufferView reading contract, sparse accessors, and malformed-input failure modes

**Rule.** `Accessor` (`Accessor.cs`) reads its `ComponentType` (`SByte`/`Byte`/`Short`/`UShort`/
`Int`/`UInt`/`Float`) and `Type` (`SCALAR`/`VEC2`/`VEC3`/`VEC4`/`MAT2`/`MAT3`/`MAT4`) from the JSON,
then delegates to `BufferView.Read` (`BufferView.cs`) to decode `Count` elements out of the
backing buffer's raw bytes, honoring an optional per-accessor `byteOffset` in addition to the
buffer view's own `ByteOffset`. A **sparse** accessor (the `"sparse"` object) first materializes a
dense base — either the ordinary `bufferView`-backed read, or, when `bufferView` is entirely
omitted from the accessor, an implicit all-zero array (`CreateZeroFilledData`) — and then overwrites
just the sparse-indexed elements in place via `ApplySparse`; a non-sparse accessor always has a
non-null `BufferView`. Sparse indices must use `Byte`, `UShort`, or `UInt` — never a signed or
float component type. Failure is loud and typed: an unrecognized numeric `componentType` value
throws `NotImplementedException` (both in `Accessor`'s own component-type read and in its sparse
`indices` sub-object); a sparse accessor's indices using a non-unsigned component type, a sparse
override index outside the accessor's own element count, or a `BufferView.Read` whose decoded
element count disagrees with the accessor's declared `Count` (`"ByteLength does not match the
accessor's count."`) all throw `InvalidOperationException`; and a `BufferView` whose JSON declares
`"byteStride"` (loosely packed / interleaved buffer data) throws `NotImplementedException`
(`"Loosely packed buffer data is not supported."`) before any byte is read — this package requires
tightly packed accessor data.

**Consequence of getting it wrong, in each direction:** Silently truncating or reinterpreting a
sparse override index that lands outside the accessor's element count (instead of throwing
`InvalidOperationException`) would corrupt an unrelated vertex's data rather than surfacing the
malformed file. Falling back to a default component type for an unrecognized `componentType` value
(instead of `NotImplementedException`) would silently misinterpret every subsequent byte in that
accessor's data as the wrong width, producing plausible-looking but wrong numbers instead of a
loud, diagnosable failure.

**Generalization.** Any future accessor-reading code this package adds (a new component type, a
new data type, or support for interleaved/loosely-packed buffers) should preserve this
fail-loud-with-a-specific-exception-type shape rather than defaulting or coercing silently — a
malformed or unsupported glTF file should never produce plausible-looking wrong data.

## Primitive attribute casting — JOINTS/WEIGHTS/TEXCOORD width and normalization

**Rule.** `Primitive.CastAsPrimitiveData` (`Primitive.cs`) determines a primitive attribute's
*output* CLR element type from the attribute's semantic (`POSITION`/`NORMAL`/`TANGENT` → `float[]`,
`TEXCOORD`/`WEIGHTS` → normalized `float[]`, `JOINTS` → `ushort[]`), not from a single hard-coded
assumption about the accessor's own `ComponentType` — because the glTF 2.0 specification legally
permits more than one source component type per semantic. `JOINTS_n` accessors are legally `Byte`
or `UShort` (literal, never-normalized joint indices); both are widened to `ushort[]` so downstream
code has one consistent type regardless of which of the two legal source types a given file
actually used. `WEIGHTS_n`/`TEXCOORD_n` accessors are legally `Float`, or a *normalized* `Byte`/
`UShort`, per glTF's normalized-integer convention (`byte.MaxValue`/`ushort.MaxValue` represents
`1.0`); float data passes through unscaled, while byte/ushort data is divided by its type's max
value. A `Primitive`'s own constructor rejects an `indices` accessor whose `Type` is not `SCALAR`
or whose `ComponentType` is not `Byte`/`UShort`/`UInt`, via `InvalidOperationException`. This
casting behavior generalizes without change to `MorphTarget`'s displacement attributes (which reuse
the same `CastAsPrimitiveData` method), except that a morph target's `TANGENT` displacement is a
3-component vector (no handedness `W`), unlike a primitive's own 4-component base `TANGENT`.

**Consequence of getting it wrong, in each direction:** Hard-coding a single element type per
attribute semantic (the historical bug this rule documents, fixed by the regression tests below) —
e.g. always assuming `WEIGHTS_0` data is already boxed as `float` — crashes with
`InvalidCastException` the moment a real file legally uses a different component type, which is
exactly what happened with a real Blender export whose `WEIGHTS_0` accessor used a normalized
unsigned byte. Applying the normalized-integer division to `JOINTS` data (instead of treating it as
literal, unnormalized indices) would silently corrupt every joint index into a near-zero float-like
value.

**Generalization.** Any future attribute semantic this package adds to `CastAsPrimitiveData` must
determine its output type and normalization behavior from the semantic's own legal component-type
set per the glTF 2.0 specification, not from an assumption about what a given exporter happens to
produce.

## Animation sampler and channel model

**Rule.** An `AnimationSampler` (`AnimationSampler.cs`) reads its keyframe `Input` timestamps as
`float`s from the referenced accessor's first component per element, defaults `Interpolation` to
`LINEAR` when the `"interpolation"` property is absent or fails to parse against the
`Interpolation` enum (`LINEAR`/`STEP`/`CUBICSPLINE`), and exposes `Output` as the raw, untyped
`ReadOnlyCollection<ReadOnlyCollection<object>>` from the referenced accessor's own `Data` — its
element shape depends on `Interpolation` (per the glTF 2.0 specification: `CUBICSPLINE` stores
three output elements per input keyframe — in-tangent, value, out-tangent — while `LINEAR`/`STEP`
store exactly one). A `Channel` (`Channel.cs`) pairs one `AnimationSampler` (looked up by index)
with one `ChannelTarget` (`ChannelTarget.cs`): an optional `Node` (nullable — the spec permits an
undefined target node when an extension defines the animated object instead, though this package
does not itself implement any such extension) and a required `Path` string naming the animated TRS
property (`"translation"`/`"rotation"`/`"scale"`) or `"weights"` for morph-target weight animation.
`Animation` (`Animation.cs`) itself is a flat pair of `Channels` and `Samplers` collections plus an
optional `Name`, with no validation that every channel's target is unique — the glTF specification
states channels within one animation "MUST NOT" repeat the same target, but this package does not
enforce that constraint itself.

**Consequence of getting it wrong, in each direction:** Defaulting to something other than `LINEAR`
for a missing or unrecognized `"interpolation"` value would silently change playback behavior for
any file that omits the property (a legal, common case — `LINEAR` is glTF's own default).
Requiring `ChannelTarget.Node` to be non-null (instead of leaving it nullable) would make this
package unable to represent a legally-shaped file whose animation target is extension-defined, even
though this package does not read that extension's data — it would simply be unable to parse the
file's structure at all rather than parsing it and leaving the extension-specific behavior
unimplemented.

**Generalization.** Any future consumer of `AnimationSampler.Output`'s untyped collection must
branch on `Interpolation` to know how many output elements correspond to each input keyframe,
exactly as `Engine-Packages/glTF/SPEC.md`'s morph/skin-animation baking already must (that
package's `GltfAnimation`/`GltfVtfAnimationBaker` own the actual playback and sampling logic;
this package only carries the parsed sampler/channel data to them, per `GLTF-05`'s cross-reference
back to this file).

## Material fields, and extras/extensions handling for unrecognized data

**Rule.** `Material` (`Material.cs`) reads `Name` (required), an optional `PBRMetallicRoughness`
(defaulting to a fresh, all-default instance when `"pbrMetallicRoughness"` is absent),
optional `NormalTexture`/`OcclusionTexture`/`EmissiveTexture`, `EmissiveFactor`, `AlphaMode`
(defaulting to `"OPAQUE"`), `AlphaCutoff` (defaulting to `0.5f`), and `DoubleSided` (defaulting to
`false`) — matching the glTF 2.0 specification's own stated defaults exactly. Beyond one specific,
documented exception, this package does **not** read `extras` or `extensions` from anywhere in a
glTF file: per this repository's own `README.md`, "general `extras` and `extensions` support…is
still not implemented." An unrecognized `extras` or `extensions` object anywhere in the file is
therefore **silently ignored** — never an error, never a warning — because the JSON-node reading
code throughout this package (`glTF.cs`, `Primitive.cs`, `Material.cs`, etc.) only ever indexes the
specific property names it knows about and never iterates "everything else" in a node, so an
unread property simply has no code path that would ever look at it. The one documented exception is
`MorphTarget`'s own, non-standard `extras.SUE_tintColor` convention (`MorphTarget.cs`) — a
Shake-Up-Games-specific application convention stored in the standard `extras` object (the location
the glTF specification itself recommends for application-specific data), read only on individual
`primitive.targets[i]` entries, never elsewhere. `SUE_tintColor` must have exactly 3 (RGB, alpha
implicitly `1.0`) or 4 (RGBA) numeric elements; any other length throws `InvalidOperationException`.

**Consequence of getting it wrong, in each direction:** If this package silently ignored a
malformed `SUE_tintColor` array instead of throwing `InvalidOperationException`, an artist-authored
tint convention with a typo'd element count would produce a wrong or missing tint at runtime with
no diagnostic pointing at the actual cause. If a future change made unrelated `extras`/`extensions`
content trigger an error instead of being silently ignored, this package would break on any
perfectly legal, conformant glTF file that happens to carry application data for a *different*
consumer — the specification explicitly permits and expects unrecognized `extras`/`extensions` to
be preserved-or-ignored by a conformant reader, never rejected.

**Generalization.** Any future non-standard convention this package adds, following
`SUE_tintColor`'s precedent, should live under `extras` (never a registered `extensions` key
without formal registration), should be read only at the specific, narrow location it applies to
(never as a blanket top-level scan), and should validate its own shape loudly (a specific
`InvalidOperationException`, naming what was expected) rather than silently tolerating a malformed
value the way an *absent* or *irrelevant* `extras` object is tolerated.

## Skin skeleton root: explicit `skin.skeleton`, and inference when it is omitted

**Rule.** `Skin` (`Skin.cs`) reads `Joints` (required), an optional `InverseBindMatrices` accessor
(when present, also back-fills each joint `Node`'s own `InverseBindMatrices` dictionary, keyed by
this skin's own index in the file's `"skins"` array), and `Skeleton`. When the file provides an
explicit `"skeleton"` index, `Skeleton` is the node at that index of the file's `"nodes"` array
(glTF 2.0 section 5.28.2: "The index of the node used as a skeleton root. The node MUST be the
closest common root of the joints hierarchy or a direct or indirect parent node of the closest
common root."), so it may be a parent of the joints that is not itself in `Joints`; it is **not**
`Joints[skeleton.Value]`, an earlier misreading that returned the wrong node whenever the skeleton
root was not at its own node index within `"joints"` and threw `ArgumentOutOfRangeException` when the
node index exceeded the joint count (`GLTFLOADER-06`). When it is omitted,
`Skeleton` is inferred as the one joint in this skin's own `Joints` list whose parent is either
`null` or **not itself a member of this skin's `Joints` list** —
`Joints.SingleOrDefault(j => j.Parent is null || !Joints.Contains(j.Parent))`. The inference
deliberately does **not** look at whether a joint's parent node happens to have an identity local
transform — that signal is unrelated to "is this the topmost bone this skin actually uses," and
using it was a real, fixed bug (see Known-broken/regression history below).

**Consequence of getting it wrong, in each direction:** The prior heuristic (checking whether a
joint's parent had no explicit TRS transform keys) threw `InvalidOperationException` ("Sequence
contains more than one matching element", from `Single()`) on any completely ordinary bone chain
where a child bone's head sits exactly at its parent's local origin — an extremely common,
unremarkable rig shape (it's what Blender produces for a simple two-bone chain with no offset) that
gives more than one joint an identity-transform parent, none of which indicates anything wrong with
the file. The current membership-based check instead correctly resolves to exactly the joint whose
parent lies *outside* this skin's own joint set, which is well-defined regardless of how many
sibling joints share an identity-transform parent.

**Generalization.** Any future "find the topmost/root element of a subset" inference this package
adds (skins, node hierarchies, or anything else expressed as a subset of a larger node graph) should
key off subset membership relative to the element's own ancestors, not off incidental transform/
naming properties of those ancestors, which the fixed bug shows are not a reliable signal.

## Buffer and image URI resolution — percent-decoding and data URIs

**Rule.** A buffer's or image's `uri` becomes bytes in exactly one place, the internal `GltfUri.Read`
(`GltfUri.cs`), following glTF 2.0 section 2.8 ("URIs"). A `uri` that starts with `data:` (ASCII
case-insensitive) is an RFC 2397 data URI, `data:[<mediatype>][;base64],<data>`, and is decoded in memory:
base64 data through base64 decoding, other data by percent-decoding to raw octets; a data URI with no comma,
or with invalid base64 text, throws (`InvalidOperationException`, `FormatException`). An image built from a
data URI exposes its media type as `Image.MIMEType`. Any other `uri` is a file path, resolved by
`GltfLoadOptions.UriDecoding` (passed as `new glTF(binDirectory, fileName, options)`; the two-argument
constructor uses `GltfLoadOptions.Default`): with `GltfUriDecoding.Standard` (the default) the percent-decoded
path is tried first and the literal, undecoded path second (skipped when the two are equal), so `my%20texture.png`
finds `my texture.png` and `100%25.png` finds `100%.png`; with `GltfUriDecoding.Literal` only the literal path is
tried, exactly as releases before the option existed did. For each candidate, an image first probes it as given
(relative to the process directory, or absolute) and then combines it with `binDirectory`; a buffer only
combines it with `binDirectory`. When no candidate exists, `FileNotFoundException` names the uri and every
path tried. Neither mode adds or removes any path containment, and URIs with other schemes are not rejected.

**Consequence of getting it wrong, in each direction:** Reading a specification-conforming `my%20texture.png`
literally fails with `FileNotFoundException` for a file that is present under its unescaped name, which is how
common exporters write any name containing a space. Percent-decoding with no literal fallback would stop
loading a non-conforming file whose real name contains a `%` sequence, and decoding first changes which file is
read when both the decoded and the literal names exist; that single collision case is why decoding is an option
(`Literal` restores the old read) rather than the only behavior, while `data:` URIs, which threw before, change
no existing read and have no option.

**Generalization.** Any future place that turns a glTF `uri` into bytes (another resource kind, a stream
overload) must go through `GltfUri.Read` rather than reading the string as a path itself, and any future change
to how an existing, working `uri` is read must sit behind a `GltfLoadOptions` setting whose default is the
specification's behavior.

## What is automated, and where

| Rule | Enforced by | Class / test |
|---|---|---|
| A dense accessor reads count/values correctly, including a shared bufferView with a per-accessor byteOffset | xUnit facts | `AccessorTests` — `ReadsADensePositionAccessor`, `HonorsAccessorByteOffsetWithinASharedBufferView` |
| A sparse accessor applies its overrides on top of a dense base, and on top of an implicit all-zero base when `bufferView` is omitted | xUnit facts | `AccessorTests` — `AppliesSparseOverridesOnTopOfADenseBufferView`, `SparseAccessorWithNoBufferViewIsZeroFilledExceptForOverrides` |
| A sparse accessor rejects a non-unsigned indices component type, and rejects an override index outside the accessor's element count | xUnit facts | `AccessorTests` — `RejectsASparseIndicesComponentTypeThatIsNotUnsigned`, `RejectsASparseOverrideIndexOutsideTheAccessorsCount` |
| `JOINTS_n` widens both legal source component types (Byte, UShort) to `ushort[]` | xUnit facts | `PrimitiveAttributeCastingTests` — `CastsJointsFromAnUnsignedByteAccessorToUShort`, `CastsJointsFromAnUnsignedShortAccessorToUShort`, `StillCastsJointsFromAByteAccessorEvenThoughOutputIsNowUShort` |
| `WEIGHTS_n` normalizes both legal integer source component types to `[0,1]` floats, and passes float data through unscaled | xUnit facts | `PrimitiveAttributeCastingTests` — `CastsWeightsFromANormalizedUnsignedByteAccessorToFloat`, `CastsWeightsFromANormalizedUnsignedShortAccessorToFloat`, `StillCastsWeightsFromAFloatAccessorUnchanged` |
| A real end-to-end file (`glTF` → `Mesh` → `Primitive`) resolves byte JOINTS_0 / normalized-byte WEIGHTS_0 through the full pipeline, not just the unit-level cast | xUnit fact | `EndToEndSkinningAttributeTests` — `LoadsAPrimitiveWithByteJointsAndNormalizedByteWeights` |
| Morph targets only accept POSITION/NORMAL/TANGENT, and a target's TANGENT is 3-component while a primitive's base TANGENT is 4-component | xUnit facts | `MorphTargetTests` — `ParsesPositionNormalAndTangentDeltas`, `RejectsATargetAttributeThatIsNotPositionNormalOrTangent`, `BasePrimitiveTangentIsFourComponentsWhileMorphTargetTangentIsThree` |
| `extras.SUE_tintColor` defaults a 3-element array's alpha to 1, reads an explicit 4-element alpha, stays null when absent, and rejects any other length | xUnit facts | `MorphTargetTests` — `DefaultsTintColorAlphaToOneWhenOnlyRgbIsGiven`, `ReadsAnExplicitTintColorAlpha`, `LeavesTintColorNullWhenNoExtrasArePresent`, `RejectsATintColorArrayOfTheWrongLength` |
| A real end-to-end file with one dense and one sparse morph target (each carrying its own `SUE_tintColor`) resolves correctly through the full `glTF` → `Mesh` → `Primitive` → `MorphTarget` pipeline, and `Mesh.Weights`/`Primitive.Targets` line up one-to-one | xUnit fact | `EndToEndMorphTargetTests` — `LoadsAPrimitiveWithADenseAndASparseMorphTarget` |
| Generalizing `Accessor`/`BufferView` to support byteOffset and sparse storage did not regress ordinary dense, non-morph content (a real fixture file) | xUnit fact | `RegressionTests` — `StillLoadsTheExistingMonkeyFixtureWithoutMorphTargets` |
| An explicit `skin.skeleton` is a `nodes` index, not a `joints` index: it resolves to the real root even when that is not the first joint, and to a parent of the joints that is not a joint itself | xUnit facts | `SkeletonExplicitTests` — `ExplicitSkeletonIsANodeIndexNotAJointsIndex`, `ExplicitSkeletonMayBeAParentOfTheJointsThatIsNotAJointItself`, `ExplicitSkeletonPointsAtTheSameNodeObjectAsTheNodesList` |
| Skeleton-root inference resolves to the joint outside this skin's own joint set, not the joint whose parent happens to have an identity transform | xUnit fact, real Blender-shaped bone chain | `SkeletonRootInferenceTests` — `InfersTheCorrectRootWhenTwoJointsHaveAnIdentityTransformParent` |
| Buffer and image uris are percent-decoded (space, non-ASCII, `%25`) with a literal fallback, the decoded file wins when both exist, `Literal` reads only the literal name, and a missing file lists the candidates tried | xUnit facts | `UriResolutionTests` — `ImageUriWithPercent20LoadsTheFileWithASpace`, `BufferUriWithPercent20LoadsTheFileWithASpace`, `NonAsciiUrisLoadWhetherPercentEncodedOrWrittenAsIs`, `EncodedPercentSignLoadsTheFileWithAPercentSign`, `LiteralNameStillLoadsWhenNoDecodedFileExists`, `DecodedFileWinsWhenBothTheDecodedAndTheLiteralFileExist`, `LiteralModeReadsOnlyTheLiteralName`, `MissingFileThrowsAndListsTheCandidatePathsTried`, `RenamedCopyOfTheMonkeyFixtureLoadsThroughAnEscapedUri` |
| `data:` URIs decode for buffers (through the full pipeline) and images (bytes and media type), percent-encoded data decodes to raw octets, and malformed data URIs throw | xUnit facts | `UriResolutionTests` — `Base64DataUriBufferDecodesThroughTheFullPipeline`, `Base64DataUriImageHasTheDecodedBytesAndMediaType`, `PercentEncodedDataUriDecodesToTheRawOctets`, `MalformedDataUrisThrowWithAMessageNamingTheProblem` |

## No automated coverage

- **The loading entry point's non-null-empty-collection contract and two-pass node linking**
  (`glTF`'s constructor, `GLTFLOADER-01`) has no dedicated unit test of its own; it is only
  exercised indirectly by every end-to-end test's `new glTF(...)` call, which does not specifically
  assert on an absent-array case or a forward-declared parent/child relationship.
- **Malformed/unsupported-input failure modes** (`GLTFLOADER-02`) — the `NotImplementedException`
  paths for an unrecognized `componentType` and for a `byteStride`-bearing (interleaved)
  `BufferView`, and the `InvalidOperationException` path for a `BufferView.Read` count mismatch —
  have no dedicated test; only the sparse-accessor-specific failure paths
  (`RejectsASparseIndicesComponentTypeThatIsNotUnsigned`, `RejectsASparseOverrideIndexOutsideTheAccessorsCount`)
  are directly tested.
- **`AnimationSampler`/`Channel`/`ChannelTarget`/`Animation`** (`GLTFLOADER-04`) have no dedicated
  test class in this project at all — nothing named for animation sampler or channel parsing
  exists in `GLTF Runtime Loader.Tests/`. This is a real coverage gap for the animation-parsing
  surface itself, distinct from the *engine-side* baking-logic tests in the separate
  `Engine-Packages/glTF/SPEC.md` package (a different repository's own test project), which cover
  code that *consumes* this package's parsed `Animation` objects, not the parsing itself.
- **`Material`'s field defaults and `PBRMetallicRoughness`/texture-info sub-object parsing**
  (`GLTFLOADER-05`) have no dedicated test class of its own; `Material` is only exercised
  indirectly wherever a test's fixture or hand-built JSON happens to include a `"materials"` array
  (e.g. the monkey-fixture regression test, which asserts only that the loaded materials
  collection is non-empty).
- **`Skin.InverseBindMatrices` back-filling each joint `Node`'s own `InverseBindMatrices`
  dictionary** (`GLTFLOADER-06`) is not directly asserted by `SkeletonRootInferenceTests`, which
  focuses only on `Skeleton` inference; no test reads a joint `Node`'s `InverseBindMatrices` value
  back out after skin construction.

## Known-broken behavior

No bug tracked in `.planning/STATE.md` or `01-BUG-CLASSIFICATION.md` applies to this package — that
classification covers the `ECS`/`Sprites` engine packages in the separate `ShakeUpEngine` repository,
not this one. Within this package's own history (visible in its git log and the regression-test
suite above), the two defects this spec documents as *fixed*, not still-broken, are: (1) the
hard-coded single-element-type assumption in `Primitive.CastAsPrimitiveData` (`GLTFLOADER-03`,
fixed, pinned by `PrimitiveAttributeCastingTests`), and (2) the identity-transform-parent
skeleton-root heuristic in `Skin` (`GLTFLOADER-06`, fixed, pinned by `SkeletonRootInferenceTests`).
No behavior currently known to be wrong exists in this package as of this spec.

Michael: please confirm that the "silently ignore all `extras`/`extensions` outside
`SUE_tintColor`" behavior (`GLTFLOADER-05`) is still the intended scope — the package's own
`README.md` TODO section calls out a future code-cleanup pass but does not mention adding general
`extras`/`extensions` support, so this spec treats the current scope as intentional rather than an
oversight, but that is a judgment call this file cannot verify by running a command.
