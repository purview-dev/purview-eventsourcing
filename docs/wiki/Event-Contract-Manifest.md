# Event Contract Manifest

The source generator produces a **deterministic, machine-readable schema manifest** of every
generated event contract in a compilation. The manifest is the machine-readable contract that
must stay compatible with previously persisted event payloads, and it is the input to
baseline-based compatibility validation.

## What is captured

For every `[Aggregate]` with at least one valid event method, the manifest records:

| Entry | Meaning |
| --- | --- |
| Aggregate name / namespace | The aggregate type identity |
| Event name / namespace / method | The generated event identity and the source method |
| Schema version | `[Event(Version = N)]`, defaulting to 1 |
| Fields | Each persisted event property: name, fully-qualified type, element type (arrays), array flag, nullability, requiredness (`[Required]`), and string flag |

The manifest is deliberately **location-free**: it captures only what affects persisted JSON
compatibility, so comments, formatting, and unrelated source edits never change it.

## Determinism guarantees

- Stable ordinal ordering for aggregates, events, and fields — reordering source declarations
  does not change the output.
- No timestamps, absolute paths, machine information, random values, reflection-order
  dependencies, or culture-sensitive formatting.
- Identical input always produces byte-identical output.

## Emitting the manifest

Emission is opt-in via the MSBuild property:

```xml
<PropertyGroup>
  <PurviewEventContractManifestEnabled>true</PurviewEventContractManifestEnabled>
</PropertyGroup>
```

With the property set, the generator emits `EventContractManifest.g.cs` (a generated source
constant) and the packaged build targets materialize the compact JSON to
`EventContractManifest.json` in the project directory after `CoreCompile`.

## Supplying a baseline

Add the approved manifest as an additional file so the generator can compare current contracts
against it:

```xml
<ItemGroup>
  <AdditionalFiles Include="EventContractManifest.json" />
</ItemGroup>
```

The default baseline file name is `EventContractManifest.json`. Override it with:

```xml
<PropertyGroup>
  <PurviewEventContractBaselineFileName>event-contracts.json</PurviewEventContractBaselineFileName>
</PropertyGroup>
```

Comparison runs whenever a matching additional file is present; without a baseline no
compatibility diagnostics are emitted.

## Generate, commit, update, and validate in CI

1. **Generate** — enable `PurviewEventContractManifestEnabled` and build; the target writes
   `EventContractManifest.json`.
2. **Commit** — commit the generated file as the approved baseline.
3. **Validate** — every build (CI included) compares current contracts against the committed
   baseline and fails on breaking changes.
4. **Update** — for an intentional, documented schema evolution, bump the schema version (and
   add an upcaster), then regenerate and commit the updated baseline in the same change.

## Compatible additions versus breaking changes

**Silent (compatible):**

- Adding a new aggregate or a new event.
- Bumping an event's schema version (the sanctioned evolution path).
- Adding an optional (nullable, non-`[Required]`) field to an existing event.
- Relaxing a field from non-nullable to nullable.

**Diagnostics (breaking), reported as errors:**

| ID | Condition |
| --- | --- |
| `EVENTSTORE030` | An aggregate contract was removed or renamed |
| `EVENTSTORE031` | An event was removed or renamed |
| `EVENTSTORE032` | A persisted field was removed or renamed |
| `EVENTSTORE033` | A persisted field type changed incompatibly |
| `EVENTSTORE034` | A field became required/non-nullable, or a `[Required]` field was added on an unchanged version |
| `EVENTSTORE035` | An event's schema version decreased below the baseline |
| `EVENTSTORE036` | The baseline manifest is malformed or uses an unsupported format version |

Each diagnostic points at the current method or aggregate declaration and explains the
remediation: retain compatibility, bump the schema version and add an upcaster, or introduce a
new event type.

## Runtime access and Admin inspection

The generated `EventContractManifest` class is public, so applications can register it for runtime
inspection:

```csharp
builder.Services.AddEventContractManifest(
	EventContractManifest.FormatVersion,
	EventContractManifest.Json,
	baselineJson: /* the committed baseline, when available */);
```

`IEventContractManifestProvider` then reports the manifest and a compatibility status
(`Compatible` when the current manifest matches the supplied baseline, `Incompatible` when it
differs, `NotConfigured` when no baseline was supplied). The Admin portal exposes it at
`GET /admin/api/manifest` when the `ViewManifest` feature and permission are enabled (opt-in,
separately authorized, audited).

## Format version

The manifest carries a `formatVersion` field. When the generator supports a different format,
the baseline is rejected with `EVENTSTORE036` and must be regenerated with the current package.