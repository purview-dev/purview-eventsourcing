namespace Purview.EventSourcing.Outbox;

/// <summary>
/// Handles a single outbox message on behalf of the dispatcher. Implementations must be idempotent:
/// an outbox guarantees at-least-once delivery, so a handler can observe the same message more than
/// once after a crash or lease expiry.
/// </summary>
public interface IOutboxHandler
{
	/// <summary>Processes one outbox message.</summary>
	Task HandleAsync(OutboxEnvelope message, CancellationToken cancellationToken);
}
