namespace Purview.EventSourcing;

/// <summary>
/// Describes the persistence guarantee provided by an event-store transaction.
/// </summary>
public enum EventStoreTransactionGuarantee
{
	/// <summary>
	/// Aggregates are saved sequentially. Processing stops at the first failure, but earlier saves are not rolled back.
	/// </summary>
	BestEffort = 0,

	/// <summary>
	/// Every enlisted aggregate is committed or rolled back as one provider-native transaction.
	/// </summary>
	Atomic = 1,
}
