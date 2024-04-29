using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.MongoDb.Snapshot;

public partial class MongoDbSnapshotEventStore<T> : IMongoDbSnapshotEventStore<T>
	where T : AggregateBase, new()
{
	readonly IEventStore<T> _eventStore;
	readonly MongoDbClient _mongoDbClient;
	readonly IOptions<MongoDbEventStoreOptions> _mongoDbOptions;

	static MongoDbSnapshotEventStore()
	{
		BsonSerializer.RegisterSerializationProvider(new MongoDbAggregateSerializationProvider());
	}

	public MongoDbSnapshotEventStore(
		Internal.INonQueryableEventStore<T> eventStore,
		IOptions<MongoDbEventStoreOptions> mongoDbOptions)
	{
		_eventStore = eventStore;
		_mongoDbOptions = mongoDbOptions;

		var collectionName = $"{TypeNameHelper.GetName(typeof(T), "aggregate")}-store";
		_mongoDbClient = new(new()
		{
			ConnectionString = _mongoDbOptions.Value.ConnectionString,
			Database = _mongoDbOptions.Value.Database,
			Collection = collectionName,
			ApplicationName = _mongoDbOptions.Value.ApplicationName
		}, null, collectionName);
	}

	async public Task ForceSaveAsync(T aggregate, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(aggregate, nameof(aggregate));

		await _mongoDbClient.UpsertAsync(aggregate, BuildPredicate(aggregate), cancellationToken);
	}

	static FilterDefinition<T> BuildPredicate(T aggregate)
	{
		var predicate = new FilterDefinitionBuilder<T>()
			.Eq(MongoDbAggregateSerializer<T>.BsonDocuemntIdPropertyName, aggregate.Id());

		return predicate;
	}
}
