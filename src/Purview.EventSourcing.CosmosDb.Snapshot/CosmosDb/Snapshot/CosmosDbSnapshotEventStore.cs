using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Internal;

namespace Purview.EventSourcing.CosmosDb.Snapshot;

public sealed partial class CosmosDbSnapshotEventStore<T> : ICosmosDbSnapshotEventStore<T>
	where T : class, IAggregate, new()
{
	readonly IEventStore<T> _eventStore;
	readonly CosmosDbClient _cosmosDbClient;
	readonly PartitionKey _partitionKey;

	readonly IOptions<CosmosDbEventStoreOptions> _cosmosDbEventStoreOptions;
	readonly Type _aggregateType = typeof(T);

	static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, string> _aggregateTypeNames = new();

	public CosmosDbSnapshotEventStore(
		// Explicitly request a non-queryable event store.
		INonQueryableEventStore<T> eventStore,
		IOptions<CosmosDbEventStoreOptions> cosmosDbEventStoreOptions,
		CosmosClient? cosmosClient = null)
	{
		_eventStore = eventStore;
		_cosmosDbEventStoreOptions = cosmosDbEventStoreOptions;
		_partitionKey = new PartitionKey(GetAggregateTypeName());

		_cosmosDbClient = new CosmosDbClient(_cosmosDbEventStoreOptions.Value, cosmosClient: cosmosClient);
	}

	/// <summary>
	/// This will upsert the aggregate regardless of it's save state in the internal event store.
	/// </summary>
	/// <param name="aggregate"></param>
	/// <param name="cancellationToken"></param>
	public Task ForceSnapshotAsync(T aggregate, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(aggregate, nameof(aggregate));

		return _cosmosDbClient.UpsertAsync(aggregate, _partitionKey, cancellationToken);
	}

	string GetAggregateTypeName()
		=> _aggregateTypeNames.GetOrAdd(_aggregateType, _ => new T().AggregateType);
}
