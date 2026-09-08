# Provider Capabilities

Event-store capabilities are exposed as a provider-neutral, queryable contract so applications and
Admin tooling can determine actual guarantees instead of inferring them from a provider name.

## Discovery

Resolve `IEventStoreCapabilitiesProvider` from dependency injection:

```csharp
public sealed class StoreHealth(IEventStoreCapabilitiesProvider capabilitiesProvider)
{
	public void Report()
	{
		var capabilities = capabilitiesProvider.GetCapabilities();
		var guarantee = capabilities.TransactionGuarantee;
		var preservesMetadata = capabilities.PreservedMetadata;
	}
}
```

Capability discovery never constructs a store or probes live storage; it only reads what was
registered. `IEventStoreCapabilitiesProvider` is always resolvable after `AddEventSourcing()` and
reports the conservative `EventStoreCapabilities.Default` until a provider registers its
capabilities.

## What is exposed

| Member | Meaning |
| --- | --- |
| `TransactionGuarantee` | `EventStoreTransactionGuarantee.Atomic` or `.BestEffort` (the same abstraction used by transaction options). |
| `SupportsEventStreams` | Whether the provider persists an append-only event stream. |
| `SupportsSnapshots` | Whether the provider stores aggregate snapshots (replay cache or query store). |
| `SnapshotSchemaVersioning` | `None`, `SingleVersion` (legacy single-shape layout), or `Versioned` (honors `[SnapshotSchemaVersion]`). |
| `PreservedMetadata` | Flags for which event metadata fields are persisted: `SchemaVersion`, `CorrelationId`, `CausationId`, `UserId`, `IdempotencyId`, `AggregateVersion`, `When`. |
| `SupportsQueries` | Whether a queryable snapshot store is available through `IQueryableEventStore`. |
| `SupportsIdempotencyMarkers` | Whether saves deduplicate on an idempotency marker. |
| `Concurrency` | `Optimistic` (conflicts rejected) or `LastWriterWins`. |
| `OperationalLimitations` | Stable limitation identifiers, for example `non-persistent` (InMemory) and `no-event-stream` (Cosmos DB). |

## Registration

Built-in providers register their truthful capabilities from their `Add*EventStore` extension
methods. Multiple registrations for the same provider are merged into the union of what is actually
available (for example SQL Server event store + snapshot query store report atomic transactions,
event streams, snapshots, and queries).

Custom providers register their own capabilities explicitly:

```csharp
services.AddEventStoreCapabilities(new EventStoreCapabilities(
	EventStoreTransactionGuarantee.BestEffort,
	SupportsEventStreams: true,
	SupportsSnapshots: false,
	SnapshotSchemaVersioning: SnapshotSchemaSupport.None,
	PreservedMetadata: PreservedEventMetadata.All,
	SupportsQueries: false,
	SupportsIdempotencyMarkers: false,
	Concurrency: ConcurrencyGuarantee.Optimistic,
	OperationalLimitations: []
));
```

Providers that register nothing are reported with `EventStoreCapabilities.Default`: best-effort
transactions, no streams, no snapshots, no queries, no idempotency, and `LastWriterWins`
concurrency. A provider is never assumed to offer stronger behavior than it implements.

## Built-in capabilities

The values below are asserted by the `Capabilities.UnitTests` contract suite so documentation and
implementation cannot drift apart.

| Provider | Transactions | Event streams | Snapshots | Snapshot versions | Metadata | Queries | Idempotency | Concurrency | Transactional outbox |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| InMemory event store | BestEffort | Yes | No | None | All | No | Yes | Optimistic | No |
| InMemory snapshot store | BestEffort | Yes | Yes | SingleVersion | All | Yes | Yes | Optimistic | No |
| SQL Server event store | Atomic | Yes | Yes | Versioned | All | No | Yes | Optimistic | Yes |
| SQL Server snapshot query store | Atomic | No | Yes | SingleVersion | None | Yes | No | Optimistic | No |
| PostgreSQL event store | Atomic | Yes | Yes | Versioned | All | No | Yes | Optimistic | Yes |
| PostgreSQL snapshot query store | Atomic | No | Yes | SingleVersion | None | Yes | No | Optimistic | No |
| Azure Storage | BestEffort | Yes | Yes | Versioned | All | No | Yes | Optimistic | No |
| MongoDB event store | BestEffort | Yes | Yes | Versioned | All | No | Yes | Optimistic | No |
| MongoDB snapshot query store | BestEffort | No | Yes | SingleVersion | None | Yes | No | Optimistic | No |
| Cosmos DB snapshot query store | BestEffort | No | Yes | SingleVersion | None | Yes | No | Optimistic | No |

The [Provider Feature Matrix](Provider-Feature-Matrix.md) summarizes the same facts for package
selection.