# Guarantees and Limitations

This page is the single authoritative summary of what the framework guarantees and where it does
not. Individual topics link to their detailed pages; do not duplicate conflicting claims elsewhere.

## Event ordering and optimistic concurrency

- Event streams are the canonical source of aggregate truth. Snapshots are replaceable
  optimizations or read models.
- Events within a stream are persisted in aggregate-version order. Writes to different aggregates
  never contend.
- Providers detect conflicting writes (optimistic concurrency) and surface them as
  `ConcurrencyException`/`IConcurrencyConflict`; see `ConcurrencyRetry` and `AggregateWriteLock`
  for retry and in-process serialization. `EventStoreCapabilities.Concurrency` reports which
  providers are optimistic versus last-writer-wins.

## Transaction guarantees and failure modes

See [Transaction Guarantees](Transaction-Guarantees.md) for the full contract.

- `EventStoreTransactionGuarantee.BestEffort`: aggregates are saved sequentially; earlier saves are
  not rolled back on failure.
- `EventStoreTransactionGuarantee.Atomic`: all enlisted aggregates commit or roll back in one
  provider-native transaction (SQL Server and PostgreSQL within one database boundary).
- A transaction that requires a stronger guarantee than the enlisted stores can provide fails
  before any save (`EventStoreTransactionGuaranteeException`). Capability discovery reports the
  actual guarantee per provider.

## Idempotency scope

- Saves deduplicate on an idempotency marker where the provider supports it
  (`EventStoreCapabilities.SupportsIdempotencyMarkers`). Idempotency is scoped to a save operation
  under a correlation/idempotency identifier; it is not a delivery guarantee for downstream
  consumers (which must be idempotent themselves).

## Metadata persistence

Providers persist and expose event metadata where supported
(`EventStoreCapabilities.PreservedMetadata`). The metadata fields are `SchemaVersion`,
`CorrelationId`, `CausationId`, `UserId`, `IdempotencyId`, `AggregateVersion`, and `When`. A field
that is not preserved by a provider is exposed as `null`/default.

## Schema evolution, manifests, and upcasters

- [Event-Versioning-Strategy.md](Event-Versioning-Strategy.md) describes how to evolve event
  schemas.
- [Event-Contract-Manifest.md](Event-Contract-Manifest.md) describes the deterministic contract
  manifest and baseline validation that fails a build on breaking changes.
- Upcasters translate legacy payloads during replay. Treat emitted event names, serialized
  payloads, schema versions, and generated method signatures as compatibility-sensitive contracts.

## Snapshot compatibility and safe rebuild

See [Snapshot-Schema-Versioning.md](Snapshot-Schema-Versioning.md).

- Snapshots must always be reconstructible from the event stream.
- `[SnapshotSchemaVersion]` and `AggregateSnapshotSchema` drive version-aware snapshot storage.
  Incompatible snapshots are ignored before deserialization and canonical event replay is used
  instead; a later snapshot-eligible save writes a compatible replacement.

## Provider capability discovery

See [Provider-Capabilities.md](Provider-Capabilities.md). Resolve
`IEventStoreCapabilitiesProvider` from DI to query transaction guarantee, snapshot behavior,
preserved metadata, query support, idempotency, concurrency, and operational limitations for the
registered stores. The [Provider Feature Matrix](Provider-Feature-Matrix.md) summarizes the same
facts for package selection.

## Admin security, metadata/payload separation, and deny-by-default

- Admin endpoints are denied by default and authorized per feature
  (`AdminFeature`, `AdminPortalPolicies`). `AdminEndpointOptions` lets a host map a feature to its
  own named authorization policy.
- `ViewEvents` grants metadata access; event **payloads** are only returned with
  `ViewEventPayloads` permission. Without it, payloads are `null`.
- Event export requires both export and payload permissions. Read permissions never imply mutation
  authority. Export is capped at `AdminProjectionOptions.MaxVersionsPerQuery`; a truncated stream is
  signaled with the `Purview-Event-Export-Truncated` response header so callers can detect partial
  exports.
- Operational endpoints (`GET /admin/api/capabilities`, `GET /admin/api/health`,
  `GET /admin/api/manifest`, `GET /admin/api/outbox/poisoned`,
  `GET /admin/api/aggregates/{aggregateType}/{aggregateId}/events/unknown`,
  `GET /admin/api/aggregates/{aggregateType}/{aggregateId}/snapshot`, and the opt-in mutation
  `POST /admin/api/aggregates/{aggregateType}/{aggregateId}/snapshot/rebuild`) are opt-in,
  separately authorized, and audited through `IAdminAuditLogger` (default in-memory; replace with a
  durable implementation in production). Health reflects whether the capability contract resolves;
  it does not probe live storage. The manifest endpoint reports the runtime event-contract manifest
  and its compatibility status against a supplied baseline. A snapshot rebuild reconstructs the
  aggregate from its canonical event stream and persists a fresh snapshot; it is idempotent and
  requires both an event-backed `IEventStore` and a registered `IQueryableEventStore`.

## Query consistency and provider-specific translation limitations

- Queryable stores are snapshot-backed read models; consistency is as-of-replay, not transactional.
- SQL snapshot translation supports deep predicates over directly mapped JSON graphs, but
  provider-converted scalar value objects may not translate deep members through `.Value`; see the
  [Provider Feature Matrix](Provider-Feature-Matrix.md) and [SQL Server Guide](SQL-Server-Guide.md)
  for exact limits.

## Unknown-event handling and recovery

- Replay of an unknown event type does not corrupt the stream: the aggregate reports
  `AggregateBase.SkippedEvents` so callers can detect partial reconstruction.
- Event-schema versioning and upcasters are the recovery path for payload evolution; the contract
  manifest prevents accidental breaking changes.
- The Admin portal can report stored event type names the runtime cannot resolve to a registered
  event type (`GET /admin/api/aggregates/{aggregateType}/{aggregateId}/events/unknown`, opt-in via
  `ViewUnknownEvents`). Legacy event types handled only by an upcaster may appear in this report
  because they are not registered current event types.