namespace Purview.EventSourcing.Outbox;

/// <summary>
/// Configuration for outbox dispatch.
/// </summary>
public sealed class OutboxDispatchOptions
{
	/// <summary>The configuration section name used to bind these options.</summary>
	public const string Section = "EventStore:Outbox";

	/// <summary>The maximum number of messages claimed per dispatch cycle.</summary>
	public int BatchSize { get; set; } = 50;

	/// <summary>The maximum number of dispatch attempts before a message is moved to the poisoned state.</summary>
	public int MaxAttempts { get; set; } = 5;

	/// <summary>How long a claimed lease is held before another dispatcher may reclaim the message.</summary>
	public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);

	/// <summary>The base backoff applied after the first failure; doubled per attempt.</summary>
	public TimeSpan RetryBackoffBase { get; set; } = TimeSpan.FromSeconds(5);

	/// <summary>The delay between dispatch cycles when no work is available.</summary>
	public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

	/// <summary>How long dispatched and poisoned messages are retained before cleanup.</summary>
	public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);

	/// <summary>
	/// Validates the option set.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when any option is outside its supported range.</exception>
	public void Validate()
	{
		if (BatchSize < 1)
			throw new InvalidOperationException("OutboxDispatchOptions.BatchSize must be >= 1.");
		if (MaxAttempts < 1)
			throw new InvalidOperationException("OutboxDispatchOptions.MaxAttempts must be >= 1.");
		if (LeaseDuration <= TimeSpan.Zero)
			throw new InvalidOperationException("OutboxDispatchOptions.LeaseDuration must be positive.");
		if (RetryBackoffBase < TimeSpan.Zero)
			throw new InvalidOperationException("OutboxDispatchOptions.RetryBackoffBase must not be negative.");
		if (PollInterval <= TimeSpan.Zero)
			throw new InvalidOperationException("OutboxDispatchOptions.PollInterval must be positive.");
		if (Retention < TimeSpan.Zero)
			throw new InvalidOperationException("OutboxDispatchOptions.Retention must not be negative.");
	}
}
