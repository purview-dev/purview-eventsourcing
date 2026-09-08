namespace Purview.EventSourcing.Outbox;

/// <summary>
/// The outcome of a single outbox dispatch cycle.
/// </summary>
public sealed record OutboxDispatchResult(int Claimed, int Dispatched, int Failed, int Poisoned);
