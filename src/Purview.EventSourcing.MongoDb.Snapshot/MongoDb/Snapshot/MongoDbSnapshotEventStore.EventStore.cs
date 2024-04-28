using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.MongoDb.Snapshot;

partial class MongoDbSnapshotEventStore<T>
{
	public Task<T> CreateAsync(string? id = null, CancellationToken cancellationToken = default)
		=> _eventStore.CreateAsync(id, cancellationToken);

	public Task<T?> GetOrCreateAsync(string? id, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
		=> _eventStore.GetOrCreateAsync(id, operationContext, cancellationToken);

	public Task<T?> GetAsync(string id, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
		=> _eventStore.GetAsync(id, operationContext, cancellationToken);

	public Task<T?> GetAtAsync(string id, int version, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
		=> _eventStore.GetAtAsync(id, version, operationContext, cancellationToken);

	public async Task<SaveResult<T>> SaveAsync(T aggregate, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
	{
		var result = await _eventStore.SaveAsync(aggregate, operationContext, cancellationToken);
		if (result)
		{
			await ForceSaveAsync(aggregate, cancellationToken);
		}

		return result;
	}

	public Task<bool> IsDeletedAsync(string id, CancellationToken cancellationToken = default)
		=> _eventStore.IsDeletedAsync(id, cancellationToken);

	public Task<T?> GetDeletedAsync(string id, CancellationToken cancellationToken = default)
		=> _eventStore.GetDeletedAsync(id, cancellationToken);

	public async Task<bool> DeleteAsync(T aggregate, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
	{
		var result = await _eventStore.DeleteAsync(aggregate, operationContext, cancellationToken);
		if (result)
		{
			await _mongoDbClient.Value.DeleteAsync(BuildPredicate(aggregate), cancellationToken);
		}

		return result;
	}

	public async Task<bool> RestoreAsync(T aggregate, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
	{
		var result = await _eventStore.RestoreAsync(aggregate, operationContext, cancellationToken);
		if (result)
		{
			await _mongoDbClient.Value.UpsertAsync(aggregate, BuildPredicate(aggregate), cancellationToken);
		}

		return result;
	}

	public IAsyncEnumerable<string> GetAggregateIdsAsync(bool includeDeleted, CancellationToken cancellationToken = default)
		=> _eventStore.GetAggregateIdsAsync(includeDeleted, cancellationToken);

	public Task<ExistsState> ExistsAsync(string id, CancellationToken cancellationToken = default)
		=> _eventStore.ExistsAsync(id, cancellationToken);

	public T FulfilRequirements(T aggregate)
		=> _eventStore.FulfilRequirements(aggregate);
}
