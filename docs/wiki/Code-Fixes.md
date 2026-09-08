# Source Generator Code Fixes

The source-generator package ships IDE code fixes alongside its diagnostics. Fixes live in a
**separate analyzer assembly** (`Purview.EventSourcing.SourceGenerator.CodeFixes`) so the core
`Purview.EventSourcing.SourceGenerator` assembly never acquires a `Microsoft.CodeAnalysis.Workspaces`
dependency.

## Assembly separation

| Assembly | Contents | Workspaces dependency |
| --- | --- | --- |
| `Purview.EventSourcing.SourceGenerator` | Incremental generators, the analyzer, and all diagnostic descriptors | None |
| `Purview.EventSourcing.SourceGenerator.CodeFixes` | `CodeFixProvider` implementations that use `DocumentEditor` / `SyntaxGenerator` | Yes (PrivateAssets) |

Both assemblies are packed under `analyzers/dotnet/cs` of `Purview.EventSourcing`. No Workspaces
assembly is shipped in the package and no Workspaces dependency flows into consumer projects. The
compiler loads the code-fix assembly without instantiating its Workspaces-dependent types; the IDE
activates them for code fixes.

## Available fixes

| Diagnostic | Fix | Notes |
| --- | --- | --- |
| `EVENTSTORE001` (aggregate must be partial) | Adds `partial` to the aggregate declaration | Trivia, nesting, generic parameters, and accessibility preserved |
| `EVENTSTORE101` (value object must be partial) | Adds `partial` to the value-object declaration | Works for record structs, structs, and classes |
| `EVENTSTORE007` (event method must be partial) | Adds `partial` to the method | |
| `EVENTSTORE021` (schema version must be positive) | Resets the version to `1` | Only when the version argument is explicit |
| `EVENTSTORE022` (duplicate schema version) | Moves the version to the next unused version on the aggregate | Only when the version argument is explicit |

Fixes use stable equivalence keys and support **Fix All** where safe. No fix is offered when a
correct correction is ambiguous (for example a renamed event contract or an incompatible payload
change); the diagnostic message provides guidance instead.

## Reference

The fixes share the diagnostic descriptors defined in the source-generator assembly via
`InternalsVisibleTo`; no diagnostic is defined in the code-fix assembly.