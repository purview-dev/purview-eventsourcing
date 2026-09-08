# Agent Prompt: Complete the Purview EventSourcing Gap Remediation

You are continuing a phased production-readiness remediation of the Purview EventSourcing framework. Work directly in this repository and finish the remaining phases described below. Do not merely analyze or propose changes: implement, test, document, and commit each phase as a cohesive conventional commit.

## Operating instructions

Read and follow the root `AGENTS.md` in full before taking any action. Also read the applicable repository skills before using their workflows:

- `.agents/skills/dotnet-tunit/SKILL.md` when writing tests.
- `.agents/skills/tunit-test-runner/SKILL.md` when running or filtering tests.
- `.agents/skills/git-conventional-commits/SKILL.md` before committing.
- Any additional skill named by `AGENTS.md` when its trigger applies.

Start by running:

```powershell
git status --short
git log -8 --oneline
```

The expected starting point is a clean `main` branch with these remediation commits at its tip:

```text
41e3185 feat: separate event payload permissions
8742e89 feat: extend admin endpoint authorization
79c3f01 feat: version aggregate snapshots
2f269da feat: persist complete event metadata
4e4eaf8 feat: make transaction guarantees explicit
```

If the worktree is not clean, preserve all existing changes and determine their ownership before editing or staging anything. Never discard user work.

## Non-negotiable requirements

1. Implement the remaining work phase by phase in the priority order below.
2. Add appropriate positive, negative, compatibility, and regression tests for every feature.
3. This repository uses TUnit and Microsoft.Testing.Platform. Use `[Test]` and awaited TUnit assertions. Do not introduce xUnit, NUnit, MSTest, FluentAssertions, or VSTest `--filter` usage.
4. All `CodeFixProvider` implementations and every component that requires `Microsoft.CodeAnalysis.Workspaces*` must live in a dedicated assembly/project separate from the source-generator/analyzer assembly.
5. The source-generator assembly must remain a `netstandard2.0` Roslyn component and must not acquire a Workspaces dependency.
6. Do not disable or suppress compiler warnings, analyzer warnings, suggestions, nullable findings, generated-code findings, or style findings. Resolve them through sound implementation, API shape, null handling, documentation, or project structure.
7. Do not add `NoWarn`, `WarningsNotAsErrors`, editorconfig exclusions, `SuppressMessage`, pragma warning directives, or equivalent mechanisms to make new findings disappear.
8. Keep public APIs backward compatible wherever practical. When a deliberate contract change is unavoidable, test and document it.
9. Keep event streams canonical. Snapshots, manifests, indexes, projections, and outbox records must remain reconstructible or operational derivatives.
10. Keep provider behavior explicit. Do not imply a guarantee or capability that an implementation cannot actually provide.
11. Update analyzer release metadata for new diagnostics and update all affected SDK documentation, wiki pages, samples, feature matrices, OpenAPI artifacts, and generated clients.
12. Build and test after every phase, review the diff, then create one cohesive conventional commit. Do not combine unrelated phases into a single commit.

## Work already completed

Do not reimplement these phases unless inspection finds a concrete defect introduced by them.

### Phase 1: explicit transaction guarantees

Commit: `4e4eaf8 feat: make transaction guarantees explicit`

Implemented:

- `EventStoreTransactionGuarantee` with `BestEffort` and `Atomic` guarantees.
- Transaction options and a specific exception for unavailable guarantees.
- Capability properties on transaction contracts using compatibility-preserving default interface members.
- An options-aware factory method that rejects unsupported atomic requirements rather than silently degrading.
- SQL Server and PostgreSQL advertise native atomic transactions.
- Transaction preflight occurs before writes.
- Tests and `docs/wiki/Transaction-Guarantees.md` plus feature-matrix/navigation updates.

### Phase 2: complete event metadata persistence and exposure

Commit: `2f269da feat: persist complete event metadata`

Implemented:

- Persisted and exposed `SchemaVersion`, `CorrelationId`, `CausationId`, and `UserId` metadata.
- Default event schema version is 1.
- Event-history/Admin projections expose stored metadata rather than placeholders.
- SQL Server and PostgreSQL entity/model/migration changes.
- Azure Storage and MongoDB record changes.
- Unknown-event fallback preserves metadata.
- Provider and documentation coverage.

Important dependency note: `Purview.Telemetry.SourceGenerator` was changed from malformed prerelease `5.0.0-prerelease.4` to stable `4.4.0`. The prerelease emitted invalid `global::global::` XML-cref output. Do not casually undo this change.

### Phase 3: snapshot schema compatibility and safe rebuild

Commit: `79c3f01 feat: version aggregate snapshots`

Implemented:

- Inherited `[SnapshotSchemaVersion(n)]`, positive-version validation, and `AggregateSnapshotSchema` resolution.
- Version-aware SQL Server, PostgreSQL, MongoDB, Azure Blob, and distributed-cache snapshot handling.
- Version 1 preserves legacy storage names.
- Incompatible snapshots are ignored before deserialization, causing canonical event replay.
- A later snapshot-eligible save writes a compatible replacement, providing safe rebuild semantics.
- Documentation in `docs/wiki/Snapshot-Schema-Versioning.md`.

### Phase 4: Admin security extensibility

Commit: `8742e89 feat: extend admin endpoint authorization`

Implemented:

- `AdminEndpointOptions` lets hosts map an `AdminFeature` to their own named authorization policy.
- Host-owned route-group and per-feature endpoint conventions.
- Built-in policies remain defaults.
- Tests prove host policy enforcement and endpoint metadata convention application.

### Phase 5: event metadata versus payload permissions

Commit: `41e3185 feat: separate event payload permissions`

Implemented:

- `AdminFeature.ViewEventPayloads` and `AdminPortalPolicies.ViewEventPayloads`.
- `ViewEvents` grants metadata access; payloads are returned as `null` without payload permission.
- Event export requires both export and payload permissions.
- Aggregate-scoped permission matching with explicit deny precedence for resource-based checks.
- Admin UI handling for unavailable payloads.
- Regenerated Admin OpenAPI document and NSwag client.
- Tests for metadata-only access and export denial.

## Current verified baseline

At the end of phase 5:

- `dotnet build src/EventSourcing.slnx --configuration Release --no-restore` succeeded with **0 warnings and 0 errors**.
- All `*.UnitTests.csproj` projects passed: **621 tests passed, 0 failed**.
- CSharpier checks on changed source areas and `git diff --check` passed.
- The Admin OpenAPI document and generated NSwag client were synchronized.
- The working tree was clean immediately after commit `41e3185`.

Maintain or improve that baseline. A phase is not complete while the build emits any warning or suggestion attributable to the work.

## Remaining phases

### Phase 6: event-contract compatibility analysis and deterministic schema manifest

Implement a durable event-contract manifest and compatibility diagnostics in the existing incremental source-generator/analyzer package without introducing Workspaces dependencies.

Required behavior:

- Produce a deterministic, machine-readable schema manifest describing generated event contracts.
- Include enough identity to distinguish aggregate, event type/name, schema version, serialized fields, field types, nullability/requiredness, and any other property that affects persisted JSON compatibility.
- Use stable ordinal ordering. Do not include timestamps, absolute paths, machine information, random values, reflection-order dependencies, or culture-sensitive formatting.
- Define a supported way for a project to supply a previously approved/baseline manifest through MSBuild/`AdditionalFiles` or another incremental-input mechanism.
- Compare the current contracts with the baseline and report precise diagnostics for breaking changes. At minimum detect:
  - removal or rename of an existing event contract;
  - reuse of an existing event identity/version for a changed payload shape;
  - removal or rename of a persisted field;
  - incompatible field type changes;
  - making a previously optional/nullable field required/non-nullable;
  - schema-version regression or conflicting version identity.
- Avoid false positives for compatible additive optional fields.
- Diagnostics must identify the relevant method/type/property location where possible and contain actionable guidance: retain compatibility, increase the version and add an upcaster, or introduce a new event type as appropriate.
- Add every new diagnostic to `AnalyzerReleases.Unshipped.md` with stable IDs and correct severity.
- Malformed or incompatible baseline manifests must produce an actionable diagnostic rather than throwing or silently skipping analysis.
- Manifest generation and baseline comparison must participate correctly in the incremental pipeline. Cancellation must be respected.
- Document how to generate, commit, update, and validate a baseline manifest in CI. Clearly distinguish compatible additions from breaking changes.

Testing expectations:

- Golden/deterministic manifest output for identical input.
- Stable ordering when source declaration order changes.
- Each breaking-change category above.
- Compatible additive optional field.
- Multiple aggregates, namespaces, nested types, explicit event names, versions, nullable types, collections, and value objects.
- Missing baseline and malformed baseline behavior.
- No diagnostics for an unchanged contract.
- Incremental invalidation tests showing unrelated syntax changes do not regenerate/recompare every aggregate unnecessarily.

Implementation guidance:

- Inspect `src/src/SourceGenerator/Generators/AggregateSourceGenerator.cs`, `AggregateInfoBuilder`, `AggregateRecords`, `DiagnosticLibrary`, and the existing generator test harness first.
- Prefer shared immutable/equatable manifest models derived from `AggregateInfo` over reparsing generated C#.
- Keep JSON serialization logic compatible with `netstandard2.0`; introduce no runtime dependency into consumer projects unless explicitly justified.
- Decide whether the emitted manifest is a generated source constant, an analyzer artifact, or an MSBuild-written file based on existing SDK/build conventions. If a build target is needed to materialize the manifest, package and test that target correctly.

Commit only after focused generator tests, the full source-generator unit suite, a warning-free solution build, formatting checks, and relevant package-content verification pass.

Suggested commit subject:

```text
feat: validate event contract compatibility
```

### Phase 7: incremental source-generator performance and regression guarantees

Strengthen correctness and performance coverage for the aggregate/value-object incremental generators.

Required behavior:

- Add regression tests that inspect Roslyn incremental step reasons or equivalent tracked outputs.
- Prove that changing one aggregate only invalidates outputs dependent on that aggregate.
- Prove that unrelated source, comments/trivia where semantically irrelevant, and unrelated additional files do not regenerate all outputs.
- Include manifest/baseline processing added in phase 6.
- Cover addition, update, removal, invalid partial code, and recovery after a compilation error.
- Ensure deterministic generated output across repeated runs.
- Extend `src/tests/SourceGenerator.PerformanceTests` with repeatable scenarios and explicit measurements for cold generation, warm unchanged rerun, and single-aggregate edit.
- Use generous, evidence-based regression thresholds that detect material regressions without creating machine-speed flaky tests. Prefer ratios, allocation bounds, or recorded benchmark assertions over unrealistically tight wall-clock limits.
- Document how to run and interpret the performance suite and record the comparison conditions.

Do not “optimize” by weakening diagnostics, dropping correctness checks, caching mutable compiler objects globally, or accessing undeclared filesystem/environment inputs.

Suggested commit subject:

```text
test: enforce incremental generator performance
```

### Phase 8: provider capability discovery

Add a provider-neutral, queryable capability model so applications and Admin tooling can determine actual guarantees instead of inferring them from provider names.

Required behavior:

- Introduce a core capability contract with stable identifiers and strongly typed values where appropriate.
- At minimum expose:
  - transaction guarantee (`Atomic` versus `BestEffort`);
  - snapshot support and snapshot schema-version behavior;
  - metadata fields preserved;
  - projection/query support relevant to public APIs;
  - idempotency/concurrency guarantees;
  - provider-specific operational limitations that affect safe use.
- Every built-in provider must register and expose truthful capabilities: InMemory, Azure Storage, Cosmos DB, MongoDB, PostgreSQL, and SQL Server.
- Custom providers need a sensible compatibility path and must not be forced to claim stronger behavior than they implement.
- Capability discovery must be available through DI without constructing a destructive operation or probing live storage.
- Reuse the transaction-guarantee abstraction from phase 1 rather than inventing a parallel enum.
- Update the provider feature matrix from executable capability definitions where practical, or add tests that keep documentation claims aligned.
- Add contract tests for every built-in provider and tests for custom/legacy provider defaults.

Suggested commit subject:

```text
feat: expose event store capabilities
```

### Phase 9: code fixes in a separate Workspaces-dependent assembly

Create a dedicated code-fix project under `src/src`, with corresponding tests under `src/tests`.

Hard boundary:

- No `Microsoft.CodeAnalysis.Workspaces*`, Features, or code-fix-only package may be referenced by `src/src/SourceGenerator`.
- The new project should contain all `CodeFixProvider` implementations and any helper that requires Workspace, DocumentEditor, SyntaxGenerator, Renamer, or equivalent APIs.
- Analyzer/generator diagnostics stay in the existing source-generator assembly.
- Verify NuGet packaging so analyzers and code-fix assemblies land in the correct analyzer tooling paths without becoming runtime dependencies.

Implement useful fixes for diagnostics where the correction is safe and unambiguous. Prioritize:

- adding `partial` to aggregate/value-object declarations;
- adding `partial` to event methods;
- correcting non-positive or missing schema version where the intended next version can be determined safely;
- generating or updating explicit event-version syntax for compatibility diagnostics when safe;
- straightforward naming/signature fixes only when semantics are not guessed.

Requirements:

- Use stable equivalence keys and support Fix All where safe.
- Preserve trivia, formatting, accessibility, nesting, generic constraints, file-scoped/block namespaces, and existing attribute arguments.
- Do not offer a fix when multiple semantic outcomes are plausible; the diagnostic message can provide guidance instead.
- Add Roslyn code-fix tests for each diagnostic/fix, including nested/generic/multi-document cases and Fix All where supported.
- Add project-reference/package-content tests proving Workspaces dependencies do not leak into the generator assembly or consumer runtime graph.
- Update package READMEs and analyzer documentation.

Suggested project names should follow repository conventions, for example `SourceGenerator.CodeFixes` and `SourceGenerator.CodeFixes.UnitTests`, but inspect SDK/package naming patterns before deciding.

Suggested commit subject:

```text
feat: add event sourcing code fixes
```

### Phase 10: documentation reorganization and missing guarantees documentation

Reorganize documentation around user journeys without deleting useful technical detail.

Required outcomes:

- A clear getting-started path from package selection through aggregate definition, persistence provider registration, save/load, tests, and production deployment.
- A dedicated “guarantees and limitations” section covering:
  - event ordering and optimistic concurrency;
  - transaction guarantees and failure modes;
  - idempotency scope;
  - metadata persistence;
  - schema evolution, manifests, and upcasters;
  - snapshot compatibility and safe rebuild;
  - provider capability discovery;
  - Admin security, metadata/payload separation, and deny-by-default behavior;
  - query consistency and provider-specific translation limitations;
  - unknown-event handling and recovery expectations.
- Reconcile `README.md`, `docs/wiki/Home.md`, provider guides, feature matrix, package SDK READMEs, source-generator behavior docs, and event-versioning docs.
- Remove duplicated/conflicting claims by linking to a single authoritative page.
- Check every code snippet against current public APIs and correct stale names.
- Add documentation link/anchor validation if the repository has a suitable existing mechanism; do not add a large toolchain solely for this.
- Document the separate analyzer/code-fix assembly and installation behavior.

This phase is documentation-focused, but run builds/tests for any executable samples or code changes made while correcting docs.

Suggested commit subject:

```text
docs: organize production guarantees guidance
```

## Optional P2 work after phases 6–10

Only begin this work after the ten primary phases are complete and verified. Keep each feature in its own commit.

### Operational Admin features

Perform a fresh gap check before implementation. Candidate features include:

- provider/capability health and readiness endpoints;
- schema-manifest inspection and compatibility status;
- snapshot status and authorized rebuild operations;
- dead-letter/unknown-event visibility;
- audit records for privileged Admin operations;
- safe pagination/export diagnostics.

Any mutating or operational endpoint must be opt-in, separately authorized, auditable, cancellation-aware, idempotent where practical, and explicit about provider support. Never let a read permission imply mutation authority.

### Transactional outbox

Design this as an explicit capability, not a universal guarantee.

- SQL Server and PostgreSQL should write outbox records in the same native database transaction as events.
- Define dispatch leasing/claiming, ordering, retry, backoff, poison-message handling, deduplication identity, cleanup/retention, observability, and cancellation behavior.
- Do not describe Azure Storage, Cosmos DB, MongoDB, or InMemory implementations as atomic unless their actual transaction boundaries prove it.
- Integrate outbox capability reporting with phase 8.
- Add real-provider integration tests for atomic commit/rollback and concurrent dispatchers. In-memory-only tests are insufficient.
- Document exactly-once versus at-least-once semantics honestly. An outbox normally provides atomic persistence plus at-least-once delivery, so consumers still need idempotency.

## Validation gates after each phase

Run focused tests first. Then, unless infrastructure makes a check impossible, run:

```powershell
dotnet build src/EventSourcing.slnx --configuration Release --no-restore
dotnet test src/EventSourcing.slnx --no-build --configuration Release --ignore-exit-code 8 -- --treenode-filter "/*UnitTest*/*/*/*"
dotnet csharpier check .
git diff --check
```

Notes:

- If dependencies or project files changed, run restore before building.
- If package/build assets, analyzers, code fixes, transitive targets, or package references changed, run pack and inspect package contents and dependency flow.
- If provider behavior depends on actual translation, transaction boundaries, serialization, concurrency, or indexes, run the affected integration tests using the real provider/Testcontainers. If Docker or credentials are unavailable, report the exact skipped suites and residual risk; do not weaken the tests.
- A zero-test filtered run is not success. Treat Microsoft.Testing.Platform exit code 8 deliberately and verify expected test discovery/counts.
- Regenerate Admin OpenAPI and client artifacts after an API contract change:

```powershell
dotnet run --project src/tools/AdminAPI.OpenAPI/AdminAPI.OpenAPI.csproj --configuration Release
dotnet nswag run src/src/Admin.Client/nswag.json
```

- Review `git diff --stat`, `git diff`, and `git status --short` before every commit.
- Stage only phase-owned files.
- Use conventional commit subjects and report the exact commit SHA.

## Quality and design review checklist

Before declaring a phase complete, explicitly audit:

- Public API and binary/source compatibility.
- Serialized event and snapshot compatibility.
- Deterministic ordering and culture independence.
- Correct cancellation propagation.
- Thread safety and absence of mutable global generator state.
- Provider parity and truthful capability claims.
- Security defaults, scoped permission behavior, and deny precedence.
- Nullable annotations and generated client/UI consumers.
- Analyzer IDs, locations, severities, release metadata, and documentation.
- Runtime dependency leakage from analyzer/code-fix packages.
- Tests that would fail if the implementation regressed, rather than tests that merely execute code.
- No warning suppression or configuration weakening.

## Final report format

When all requested work is complete, provide a concise but concrete report containing:

1. Each phase and its commit SHA/subject.
2. The principal public APIs and behavior added.
3. Important files/projects created or changed.
4. Focused and broad test results with counts.
5. Release build, formatting, pack, and package-content results.
6. Integration suites run, skipped, or blocked, including exact infrastructure reasons.
7. Any deliberate compatibility decisions or documented limitations.
8. Whether optional operational Admin/outbox work was completed, deferred, or found unnecessary after reanalysis.

Do not claim completion while a required phase, warning, failing test, unsynchronized generated artifact, or undocumented guarantee remains.
