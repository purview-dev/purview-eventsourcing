using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Purview.EventSourcing.Outbox;

/// <summary>
/// Claims and dispatches outbox messages in lease-protected batches.
/// </summary>
public interface IOutboxDispatcher
{
	/// <summary>
	/// Runs one dispatch cycle: claims a batch, dispatches each message, and marks outcomes.
	/// </summary>
	Task<OutboxDispatchResult> DispatchAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IOutboxDispatcher"/> that delegates each message to an
/// <see cref="IOutboxHandler"/>, applies exponential backoff on failure, and moves messages to the
/// poisoned state once the retry budget is exhausted.
/// </summary>
public sealed partial class OutboxDispatcher(
	IOutboxStore store,
	IOutboxHandler handler,
	IOptions<OutboxDispatchOptions> options,
	ILogger<OutboxDispatcher> logger
) : IOutboxDispatcher
{
	/// <inheritdoc/>
	public async Task<OutboxDispatchResult> DispatchAsync(CancellationToken cancellationToken)
	{
		var settings = options.Value;
		var leaseOwner = $"{Environment.MachineName}:{Guid.NewGuid():N}";
		var leaseUntil = DateTimeOffset.UtcNow + settings.LeaseDuration;

		var claimed = await store.ClaimNextBatchAsync(leaseOwner, leaseUntil, settings.BatchSize, cancellationToken);

		var dispatched = 0;
		var failed = 0;
		var poisoned = 0;

		foreach (var message in claimed)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				await handler.HandleAsync(message, cancellationToken);
				await store.CompleteAsync(message.Id, cancellationToken);
				dispatched++;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			// The handler can throw any exception; each failure transitions the message to failed or
			// poisoned rather than terminating the dispatch cycle.
#pragma warning disable CA1031
			catch (Exception ex)
#pragma warning restore CA1031
			{
				var nextAttempt = message.AttemptCount + 1;
				if (nextAttempt >= settings.MaxAttempts)
				{
					await store.MarkPoisonedAsync(message.Id, ex.Message, cancellationToken);
					poisoned++;
					LogPoisoned(logger, message.Id, nextAttempt, ex.Message);
				}
				else
				{
					var nextAttemptUtc = DateTimeOffset.UtcNow + ComputeBackoff(settings.RetryBackoffBase, nextAttempt);
					await store.MarkFailedAsync(message.Id, ex.Message, nextAttemptUtc, cancellationToken);
					failed++;
					LogFailed(logger, message.Id, nextAttempt, ex.Message);
				}
			}
		}

		var claimedCount = claimed.Count;
		if (claimedCount > 0)
			LogDispatchCycle(logger, claimedCount, dispatched, failed, poisoned);

		return new OutboxDispatchResult(claimedCount, dispatched, failed, poisoned);
	}

	internal static TimeSpan ComputeBackoff(TimeSpan baseDelay, int attempt) =>
		baseDelay * Math.Pow(2, Math.Min(attempt - 1, 6));

	[LoggerMessage(
		LogLevel.Warning,
		Message = "Outbox message {OutboxMessageId} moved to poisoned state after {Attempts} attempts: {OutboxError}"
	)]
	private static partial void LogPoisoned(ILogger logger, string outboxMessageId, int attempts, string outboxError);

	[LoggerMessage(
		LogLevel.Warning,
		Message = "Outbox message {OutboxMessageId} failed on attempt {Attempt}: {OutboxError}"
	)]
	private static partial void LogFailed(ILogger logger, string outboxMessageId, int attempt, string outboxError);

	[LoggerMessage(
		LogLevel.Information,
		Message = "Outbox dispatch cycle claimed {Claimed}, dispatched {Dispatched}, failed {Failed}, poisoned {Poisoned}"
	)]
	private static partial void LogDispatchCycle(ILogger logger, int claimed, int dispatched, int failed, int poisoned);
}
