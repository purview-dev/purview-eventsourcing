using System.Collections.Immutable;

namespace Purview.EventSourcing;

/// <summary>
/// Identifies which event metadata fields a provider persists and exposes after a save.
/// </summary>
[Flags]
public enum PreservedEventMetadata
{
	/// <summary>No event metadata is preserved beyond the payload.</summary>
	None = 0,

	/// <summary>The idempotency identifier used to deduplicate saves.</summary>
	IdempotencyId = 1 << 0,

	/// <summary>The aggregate version at which the event was applied.</summary>
	AggregateVersion = 1 << 1,

	/// <summary>The UTC timestamp recorded when the event was applied.</summary>
	When = 1 << 2,

	/// <summary>The identifier of the user that caused the event.</summary>
	UserId = 1 << 3,

	/// <summary>The upstream event identifier that caused this event.</summary>
	CausationId = 1 << 4,

	/// <summary>The correlation identifier grouping a set of causally related events.</summary>
	CorrelationId = 1 << 5,

	/// <summary>The event payload schema version.</summary>
	SchemaVersion = 1 << 6,

	/// <summary>Every metadata field supported by the framework.</summary>
	All = (1 << 7) - 1,
}

/// <summary>
/// Describes how a provider treats aggregate snapshot schema versions.
/// </summary>
public enum SnapshotSchemaSupport
{
	/// <summary>The provider does not store aggregate snapshots.</summary>
	None = 0,

	/// <summary>Snapshots are stored under the legacy single-shape layout (schema version 1).</summary>
	SingleVersion = 1,

	/// <summary>Snapshots honor <c>[SnapshotSchemaVersion]</c> with distinct storage per version.</summary>
	Versioned = 2,
}

/// <summary>
/// Describes the concurrency behavior a provider guarantees for aggregate streams.
/// </summary>
public enum ConcurrencyGuarantee
{
	/// <summary>Conflicting writes are detected and rejected (optimistic concurrency).</summary>
	Optimistic = 0,

	/// <summary>The last write wins; conflicts are not detected by the provider.</summary>
	LastWriterWins = 1,
}

/// <summary>
/// Stable, machine-readable identifiers for provider-specific operational limitations reported
/// through <see cref="EventStoreCapabilities.OperationalLimitations"/>.
/// </summary>
public static class EventStoreOperationalLimitation
{
	/// <summary>Data is not persisted across restarts (in-memory store).</summary>
	public const string NonPersistent = "non-persistent";

	/// <summary>The provider does not persist an event stream (snapshot-only).</summary>
	public const string NoEventStream = "no-event-stream";
}

/// <summary>
/// The truthful, provider-neutral capability contract for an event-store implementation. Capabilities
/// are discovered through dependency injection without constructing the store or probing storage, so
/// applications and Admin tooling can determine actual guarantees rather than inferring them from a
/// provider name.
/// </summary>
/// <param name="TransactionGuarantee">The persistence guarantee provided by provider-native transactions.</param>
/// <param name="SupportsEventStreams">Whether the provider persists an append-only event stream.</param>
/// <param name="SupportsSnapshots">Whether the provider stores aggregate snapshots (replay cache or query store).</param>
/// <param name="SnapshotSchemaVersioning">How snapshot schema versions are handled.</param>
/// <param name="PreservedMetadata">Which event metadata fields are persisted and exposed.</param>
/// <param name="SupportsQueries">Whether a queryable snapshot store is available through <c>IQueryableEventStore</c>.</param>
/// <param name="SupportsIdempotencyMarkers">Whether saves deduplicate on an idempotency marker.</param>
/// <param name="Concurrency">The concurrency behavior for aggregate streams.</param>
/// <param name="OperationalLimitations">Stable, machine-readable limitation identifiers that affect safe use.</param>
public sealed record EventStoreCapabilities(
	EventStoreTransactionGuarantee TransactionGuarantee,
	bool SupportsEventStreams,
	bool SupportsSnapshots,
	SnapshotSchemaSupport SnapshotSchemaVersioning,
	PreservedEventMetadata PreservedMetadata,
	bool SupportsQueries,
	bool SupportsIdempotencyMarkers,
	ConcurrencyGuarantee Concurrency,
	ImmutableArray<string> OperationalLimitations
)
{
	/// <summary>
	/// The conservative baseline used when no provider registers capabilities. Custom and legacy
	/// providers are never assumed to offer stronger guarantees than this.
	/// </summary>
	public static EventStoreCapabilities Default { get; } =
		new(
			EventStoreTransactionGuarantee.BestEffort,
			SupportsEventStreams: false,
			SupportsSnapshots: false,
			SnapshotSchemaVersioning: SnapshotSchemaSupport.None,
			PreservedMetadata: PreservedEventMetadata.None,
			SupportsQueries: false,
			SupportsIdempotencyMarkers: false,
			Concurrency: ConcurrencyGuarantee.LastWriterWins,
			[]
		);

	/// <summary>
	/// Whether the provider can persist outbox messages atomically with event saves (transactional
	/// outbox). When <see langword="false"/>, outbox writes are not guaranteed to share the event
	/// transaction boundary.
	/// </summary>
	public bool SupportsTransactionalOutbox { get; init; }

	/// <summary>
	/// Combines capability registrations into a single effective contract. The strongest applicable
	/// guarantee wins, so an application that registers an event store and a query snapshot store for
	/// the same provider reports the union of what is actually available.
	/// </summary>
	public static EventStoreCapabilities Merge(IEnumerable<EventStoreCapabilities> parts)
	{
		var all = parts.ToImmutableArray();
		if (all.IsEmpty)
			return Default;

		if (all.Length == 1)
			return all[0];

		// Merge the strongest guarantees from all parts, and union the operational limitations.
		return new EventStoreCapabilities(
			(EventStoreTransactionGuarantee)all.Max(static part => (int)part.TransactionGuarantee),
			all.Any(static part => part.SupportsEventStreams),
			all.Any(static part => part.SupportsSnapshots),
			(SnapshotSchemaSupport)all.Max(static part => (int)part.SnapshotSchemaVersioning),
			all.Aggregate(PreservedEventMetadata.None, static (current, part) => current | part.PreservedMetadata),
			all.Any(static part => part.SupportsQueries),
			all.Any(static part => part.SupportsIdempotencyMarkers),
			all.Any(static part => part.Concurrency == ConcurrencyGuarantee.LastWriterWins)
				? ConcurrencyGuarantee.LastWriterWins
				: ConcurrencyGuarantee.Optimistic,
			[
				.. all.SelectMany(static part => part.OperationalLimitations)
					.Distinct(StringComparer.Ordinal)
					.OrderBy(static limitation => limitation, StringComparer.Ordinal),
			]
		)
		{
			SupportsTransactionalOutbox = all.Any(static part => part.SupportsTransactionalOutbox),
		};
	}
}
