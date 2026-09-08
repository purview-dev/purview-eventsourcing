namespace Purview.EventSourcing.Outbox;

/// <summary>
/// A message queued in the transactional outbox for reliable dispatch to a downstream consumer.
/// </summary>
/// <param name="Id">The stable message identifier.</param>
/// <param name="AggregateType">The aggregate type that produced the message.</param>
/// <param name="AggregateId">The aggregate id that produced the message.</param>
/// <param name="EventType">The event type name this message represents.</param>
/// <param name="PayloadJson">The serialized payload dispatched to the consumer.</param>
/// <param name="IdempotencyKey">
/// An application-supplied deduplication identity. Enqueuing a second message with the same key is a
/// no-op; consumers should also be idempotent because an outbox guarantees at-least-once delivery.
/// </param>
/// <param name="CorrelationId">An optional correlation identifier for end-to-end tracing.</param>
/// <param name="CreatedUtc">The UTC timestamp the message was enqueued.</param>
public sealed record OutboxEnvelope(
	string Id,
	string AggregateType,
	string AggregateId,
	string EventType,
	string PayloadJson,
	string? IdempotencyKey,
	string? CorrelationId,
	DateTimeOffset CreatedUtc
)
{
	/// <summary>The current lifecycle state.</summary>
	public OutboxState State { get; init; } = OutboxState.Pending;

	/// <summary>The number of failed dispatch attempts.</summary>
	public int AttemptCount { get; init; }

	/// <summary>The earliest UTC time the message may be claimed again after a failure.</summary>
	public DateTimeOffset? NextAttemptUtc { get; init; }

	/// <summary>The UTC time the message was successfully dispatched, when applicable.</summary>
	public DateTimeOffset? DispatchedUtc { get; init; }

	/// <summary>The UTC time the current dispatch lease expires.</summary>
	public DateTimeOffset? LeaseExpiresUtc { get; init; }

	/// <summary>The most recent dispatch error, when applicable.</summary>
	public string? LastError { get; init; }
}
