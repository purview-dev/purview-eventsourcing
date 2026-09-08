namespace Purview.EventSourcing.Outbox;

/// <summary>
/// Persists and leases outbox messages. Implementations decide their own storage; providers backed
/// by a relational database write messages inside the same native transaction as event saves for
/// atomic persistence.
/// </summary>
public interface IOutboxStore
{
	/// <summary>
	/// Enqueues a message. When <see cref="OutboxEnvelope.IdempotencyKey"/> is set and a message with
	/// the same key already exists, the enqueue is a no-op.
	/// </summary>
	/// <returns>The number of rows actually inserted (0 when deduplicated).</returns>
	Task<int> EnqueueAsync(OutboxEnvelope message, CancellationToken cancellationToken);

	/// <summary>
	/// Atomically claims up to <paramref name="batchSize"/> messages, ordered by creation time, by
	/// assigning them to <paramref name="leaseOwner"/> until <paramref name="leaseUntil"/>. Messages
	/// currently leased by another owner, or not yet eligible for retry, are skipped.
	/// </summary>
	Task<IReadOnlyList<OutboxEnvelope>> ClaimNextBatchAsync(
		string leaseOwner,
		DateTimeOffset leaseUntil,
		int batchSize,
		CancellationToken cancellationToken
	);

	/// <summary>Marks a message as successfully dispatched.</summary>
	Task CompleteAsync(string id, CancellationToken cancellationToken);

	/// <summary>
	/// Marks a message as failed and schedules the next retry attempt.
	/// </summary>
	Task MarkFailedAsync(
		string id,
		string errorMessage,
		DateTimeOffset nextAttemptUtc,
		CancellationToken cancellationToken
	);

	/// <summary>Moves a message to the poisoned (dead-letter) state after exhausting retries.</summary>
	Task MarkPoisonedAsync(string id, string errorMessage, CancellationToken cancellationToken);

	/// <summary>
	/// Removes terminal messages (dispatched or poisoned) older than <paramref name="retention"/>.
	/// </summary>
	Task<int> CleanupAsync(TimeSpan retention, CancellationToken cancellationToken);

	/// <summary>
	/// Returns a page of poisoned (dead-letter) messages ordered most-recently-poisoned first.
	/// Providers that do not support dead-letter inspection return an empty page.
	/// </summary>
	/// <param name="skip">The number of poisoned messages to skip.</param>
	/// <param name="take">The maximum number of poisoned messages to return.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	Task<IReadOnlyList<OutboxEnvelope>> GetPoisonedAsync(int skip, int take, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(skip);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);
		return Task.FromResult<IReadOnlyList<OutboxEnvelope>>([]);
	}
}
