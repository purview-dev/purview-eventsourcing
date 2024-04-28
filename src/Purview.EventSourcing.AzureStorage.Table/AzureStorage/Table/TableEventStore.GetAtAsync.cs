using System.Diagnostics;

namespace Purview.EventSourcing.AzureStorage.Table;

partial class TableEventStore<T>
{
	public async Task<T?> GetAtAsync(string aggregateId, int version, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId, nameof(aggregateId));

		operationContext ??= EventStoreOperationContext.DefaultContext;

		_eventStoreLog.GetAggregateAtSpecificVersionStart(aggregateId, version, _aggregateTypeFullName);
		var getStopwatch = Stopwatch.StartNew();
		try
		{
			var streamVersion = await GetStreamVersionAsync(aggregateId, true, cancellationToken);
			if (streamVersion == null)
				return null;

			if (!ReturnAggregate(streamVersion.IsDeleted, aggregateId, operationContext))
				return null;

			var aggregate = new T
			{
				Details = {
					Id = aggregateId
				}
			};

			await GetAndApplyEventsAsync(aggregate, streamVersion, version, cancellationToken);

			// Make sure we lock the aggregate to prevent saving/ modifications etc.
			aggregate.Details.Locked = true;

			return FulfilRequirements(aggregate);
		}
		catch (Exception ex)
		{
			_eventStoreLog.GetAggregateAtSpecificVersionFailed(aggregateId, _aggregateTypeFullName, version, ex);
			throw;
		}
		finally
		{
			getStopwatch.Stop();
			_eventStoreLog.GetAggregateAtSpecificVersionComplete(aggregateId, _aggregateTypeFullName, version, getStopwatch.ElapsedMilliseconds);
		}
	}
}
