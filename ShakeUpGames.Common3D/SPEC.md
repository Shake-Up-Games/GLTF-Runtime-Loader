# ShakeUpGames.Common3D

**Indexes:** `COMMON3D-01`, `COMMON3D-02`, `COMMON3D-03`

This is the behavior contract for the `ShakeUpGames.Common3D` package — a tiny, dependency-free
class library of shared 3D data structures consumed by both the `GLTF Runtime Loader` package (in
this same repository) and, across a repository boundary, `Engine-Packages/Effects` in the
`ShakeUpEngine` repository. It states what behavior is guaranteed, indexed by REQ-ID, and how that
guarantee is enforced by tests. This package has **no `README.md` of its own** — there is none at
`GLTF-Runtime-Loader/ShakeUpGames.Common3D/` — so, per `SPEC-TEMPLATE.md`'s additive rule, this
file's claims are seeded directly from the package's own two source files and its `.csproj`, not
from any pre-existing document.

Cross-references `GLTF Runtime Loader/SPEC.md`'s `GLTFLOADER-NN` claims (this same repository, plan
`05-07`) for how `VertexAttribute` is actually used as a dictionary key when parsing primitive and
morph-target attribute data — that usage is the loader package's concern, not restated here.

## `PrimitiveAttributes`: explicit numeric enum values are a cross-assembly binary-compatibility requirement

**Rule.** `PrimitiveAttributes` (`PrimitiveAttributes.cs`) is a flat enum — `POSITION = 0`,
`NORMAL = 1`, `TANGENT = 2`, `TEXCOORD = 3`, `JOINTS = 4`, `WEIGHTS = 5` — with every member's
numeric value assigned **explicitly**, deliberately, rather than left to implicit sequential
assignment. The XML doc comment on the enum itself records why: this type is consumed across an
assembly boundary — `GLTF Runtime Loader` is compiled against a specific `ShakeUpGames.Common3D`
version, but the actual assembly a consumer's runtime loads is resolved independently via NuGet —
so implicit sequential values are unsafe the moment either assembly is rebuilt or republished
independently of the other. This is not a hypothetical: inserting `TANGENT` in the middle of the
enum previously shifted every subsequent member's implicit numeric value, and a real production
bug resulted when a `GLTF Runtime Loader` build compiled against the post-`TANGENT` numeric values
was evaluated at runtime against a pre-`TANGENT`-published `ShakeUpGames.Common3D` build whose
members still held their old values — `JOINTS`/`WEIGHTS` switch statements silently matched the
wrong case.

**Consequence of getting it wrong, in each direction:** Reverting to implicit sequential enum
values (even temporarily, or "just for this one insertion") reintroduces exactly the
cross-assembly-version mismatch bug this rule exists to prevent — republishing a consistent pair of
packages fixes the immediate symptom but does not prevent recurrence the next time either assembly
changes independently, since nothing in the type system enforces "these two independently-versioned
assemblies must always agree on enum numeric values" except discipline at the point new members are
added.

**Generalization.** Any future member added to `PrimitiveAttributes` must be given an explicit,
never-before-used numeric value, appended rather than inserted into the existing sequence (an
insertion has no effect on already-assigned explicit values, but is still worth avoiding for
readability). Any *other* enum this package or a sibling package defines that crosses the same
`GLTF Runtime Loader` ↔ `ShakeUpGames.Common3D` (or the cross-repo `Effects`) assembly boundary
should adopt the same explicit-value discipline for the same reason.

## `VertexAttribute`: value-type identity is `(Type, UsageIndex)`, used as a dictionary key across consumers

**Rule.** `VertexAttribute` (`VertexAttribute.cs`) is a `readonly struct` pairing a
`PrimitiveAttributes Type` with an optional `int? UsageIndex` (e.g. `TEXCOORD_1` → `Type =
TEXCOORD, UsageIndex = 1`), implementing `IEquatable<VertexAttribute>` with value equality over
both fields (`Type == other.Type && UsageIndex == other.UsageIndex`) and a matching
`GetHashCode()` (`HashCode.Combine` over both fields, treating a null `UsageIndex` as `0` for
hashing purposes only — not for equality, where `null` and `0` remain distinct because `int?`
equality itself distinguishes them). `ToString()` renders as `"{Type}"` when `UsageIndex` is null,
or `"{Type}_{UsageIndex}"` otherwise, matching glTF's own `SEMANTIC_n` naming convention. This
struct's entire purpose is to serve as a dictionary key — `GLTF Runtime Loader`'s `Primitive` and
`MorphTarget` types (`GLTFLOADER-03`) key their per-vertex attribute dictionaries on
`VertexAttribute` instances, so two lookups for "the same attribute" (e.g. two separate calls
constructing `new VertexAttribute(PrimitiveAttributes.TEXCOORD, 1)`) must compare equal and hash
identically for dictionary lookups to work at all.

**Consequence of getting it wrong, in each direction:** A `GetHashCode()` implementation that
diverges from `Equals` (the classic C# equality-contract violation) would make two equal
`VertexAttribute` values hash to different buckets, causing dictionary lookups to silently miss
even when the exact same logical attribute was looked up twice — the failure mode would look like
"this attribute isn't in the primitive" for an attribute that actually is present, with no
exception to point at the real cause. Treating a `null` `UsageIndex` as equal to an explicit `0`
(instead of keeping them distinct, as `int?`'s own equality already does) would conflate an
un-suffixed attribute like plain `TEXCOORD` with an explicitly-indexed `TEXCOORD_0`, which the glTF
specification treats as the same semantic in practice but which this struct's own field shapes
(nullable vs. non-nullable) suggest should stay distinguishable at this layer.

**Generalization.** Any future value type this package or a sibling package adds for use as a
dictionary or set key must keep `Equals`/`GetHashCode` in agreement over the exact same field set,
matching the pattern this struct already establishes — and should generally prefer the same
"read-only struct with `IEquatable<T>`" shape, since dictionary-key value types are the specific
case this package exists to hold.

## Cross-repository consumer coupling: `Engine-Packages/Effects` depends on this package with no shared build

**Rule.** `Engine-Packages/Effects/Effects.csproj`, in the separate `ShakeUpEngine` repository,
references this package via a relative-path `<ProjectReference
Include="..\..\..\OpenSource\GLTF-Runtime-Loader\ShakeUpGames.Common3D\ShakeUpGames.Common3D.csproj"
/>` — a **source-level** project reference across the symlinked repository boundary, not merely a
versioned NuGet package reference. `Effects`' `CustomEffect.SupportedPrimitiveAttributes` and
`IEffectSupportedPrimitiveAttributes.SupportedPrimitiveAttributes` (both `EffectInterfaces.cs`,
`Engine-Packages/Effects`) are typed directly as `IEnumerable<PrimitiveAttributes>` — this
package's own enum, not a copy or a re-declared equivalent. Because these are two independent git
repositories with two independent build/CI surfaces, **a breaking change to `PrimitiveAttributes`
or `VertexAttribute` made and committed in this repository is invisible to `ShakeUpEngine`'s own
build until someone explicitly builds `Effects.csproj` (or something that depends on it) against
the updated source** — no single build, test run, or CI pipeline spans both repositories to catch
it automatically.

**Consequence of getting it wrong, in each direction:** Renaming, removing, or renumbering a
`PrimitiveAttributes` member in this repository — even one that passes every test in *this*
repository's own (nonexistent, per this file's own coverage section) or sibling packages' test
suites — would silently break `Effects`' compile the next time someone builds the engine repository
against this package's updated source, with the actual root cause (a change made in a different
git repository, days or weeks earlier) far from obvious to whoever hits the resulting build
failure. This is exactly the class of bug `COMMON3D-01`'s explicit-enum-value discipline defends
against for the *runtime* mismatch case; this rule names the parallel *build-time* hazard, which
explicit enum values do nothing to prevent.

**Generalization.** Any future change to this package's public surface (new members, changed
signatures, or anything else `Effects.csproj` or the `GLTF Runtime Loader` package's own source
consumes) must be checked against both consumers before being considered complete — a green build
in this repository alone is not sufficient evidence that the change is safe, precisely because the
one existing cross-repo consumer's own build is the only thing that would ever catch a regression
here.

## No automated coverage

This package has **no test project** — per `SPEC-TEMPLATE.md`'s Part 3 coverage-honesty rules,
`ShakeUpGames.Common3D` is explicitly listed among the units with no test project at all, and this
file confirms that by looking (`ls` on this package's directory shows only its two source files and
`.csproj`, no `*.Tests` sibling anywhere in this repository). Every claim in this file is therefore
judgment-tier, not test-enforced:

- `PrimitiveAttributes`' explicit-value discipline (`COMMON3D-01`) has no test asserting the
  numeric values themselves stay fixed; a future accidental renumbering would only be caught by
  code review or by the cross-repo build failure described in `COMMON3D-03`, not by any automated
  check in this repository.
- `VertexAttribute`'s equality/hash-code contract (`COMMON3D-02`) has no unit test exercising
  `Equals`/`GetHashCode`/`ToString` directly in this repository. It is only exercised indirectly,
  through the sibling `GLTF Runtime Loader` package's own test project's use of `VertexAttribute`
  instances as dictionary keys (see `GLTF Runtime Loader/SPEC.md`'s claims-to-tests table for that
  package's own citations) — those tests would fail if the equality contract were broken, but none
  of them names this package's types as their subject, so no test class from that project is cited
  here as this package's own coverage.
- The cross-repo coupling itself (`COMMON3D-03`) is, by definition, not something any single
  repository's test suite can verify — no CI configuration in either repository currently builds
  both repositories together to catch a breaking change before it lands.

Michael: please confirm this package's total absence of a `.Tests` project is intentional scope
(a package this small, with two simple data types, may genuinely not need one) rather than an
oversight — and separately, please confirm whether a cross-repo CI check (building `Effects.csproj`
against this repository's `main` on every push here, or vice versa) is worth adding given
`COMMON3D-03`'s documented gap; this file can name the gap but cannot judge whether closing it is
worth the infrastructure cost.
