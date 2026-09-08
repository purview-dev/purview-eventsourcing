namespace Purview.EventSourcing.Outbox;

/// <summary>
/// The lifecycle state of an outbox message.
/// </summary>
public enum OutboxState
{
	/// <summary>Queued and awaiting dispatch.</summary>
	Pending = 0,

	/// <summary>Successfully dispatched.</summary>
	Dispatched = 1,

	/// <summary>A transient dispatch failure occurred and the message will be retried.</summary>
	Failed = 2,

	/// <summary>Dispatch exceeded the retry budget and was moved to the dead-letter state.</summary>
	Poisoned = 3,
}
