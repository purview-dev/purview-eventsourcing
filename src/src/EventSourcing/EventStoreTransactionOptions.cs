namespace Purview.EventSourcing;

/// <summary>
/// Configures a provider-neutral event-store transaction.
/// </summary>
public sealed record EventStoreTransactionOptions
{
	/// <summary>
	/// Gets the optional correlation ID shared by enlisted aggregate saves.
	/// </summary>
	public string? CorrelationId { get; init; }

	/// <summary>
	/// Gets the minimum persistence guarantee required when the transaction commits.
	/// </summary>
	/// <remarks>
	/// The default preserves the historical sequential fallback. Set this to
	/// <see cref="EventStoreTransactionGuarantee.Atomic"/> to reject incompatible stores before any save is attempted.
	/// </remarks>
	public EventStoreTransactionGuarantee RequiredGuarantee { get; init; }
}
