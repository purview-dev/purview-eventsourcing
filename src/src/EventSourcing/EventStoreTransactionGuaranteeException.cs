namespace Purview.EventSourcing;

/// <summary>
/// The enlisted stores cannot provide the transaction guarantee required by the caller.
/// </summary>
/// <remarks>
/// Creates an exception for an unavailable transaction guarantee.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
public sealed class EventStoreTransactionGuaranteeException(
	EventStoreTransactionGuarantee requiredGuarantee,
	EventStoreTransactionGuarantee availableGuarantee
)
	: InvalidOperationException(
		$"The transaction requires the '{requiredGuarantee}' guarantee, but the enlisted stores provide '{availableGuarantee}'. No saves were attempted."
	)
{
	/// <summary>Gets the guarantee requested by the caller.</summary>
	public EventStoreTransactionGuarantee RequiredGuarantee { get; } = requiredGuarantee;

	/// <summary>Gets the strongest guarantee available from the enlisted stores.</summary>
	public EventStoreTransactionGuarantee AvailableGuarantee { get; } = availableGuarantee;
}
