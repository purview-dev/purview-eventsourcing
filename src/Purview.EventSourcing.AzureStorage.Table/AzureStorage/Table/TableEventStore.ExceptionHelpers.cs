using Purview.EventSourcing.AzureStorage.Table.Exceptions;

namespace Purview.EventSourcing.AzureStorage.Table;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter")]
partial class TableEventStore<T>
{
	static ArgumentNullException NullAggregate(T? aggregate)
		=> new(nameof(aggregate));

	static AggregateIsDeletedException AggregateIsDeletedException(string aggregateId)
		=> new(aggregateId);

	static AggregateNotDeletedException AggregateNotDeletedException(string aggregateId)
		=> new(aggregateId);
}
