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
	readonly IMongoDbSnapshotEventStoreTelemetry _telemetry;

	readonly string _aggregateName;

	static MongoDbSnapshotEventStore()
	{
		BsonSerializer.RegisterSerializationProvider(new MongoDbAggregateSerializationProvider());
	}

	public MongoDbSnapshotEventStore(
		Internal.INonQueryableEventStore<T> eventStore,
		IOptions<MongoDbEventStoreOptions> mongoDbOptions,
		IMongoDbSnapshotEventStoreTelemetry telemetry)
	{
		_eventStore = eventStore;
		_mongoDbOptions = mongoDbOptions;
		_telemetry = telemetry;

		_aggregateName = TypeNameHelper.GetName(typeof(T), "Aggregate");
		var collectionName = _mongoDbOptions.Value.Collection ?? $"snapshot-{_aggregateName}-store";
		_mongoDbClient = new(new()
		{
			ConnectionString = _mongoDbOptions.Value.ConnectionString,
			Database = _mongoDbOptions.Value.Database,
			Collection = collectionName,
			ApplicationName = _mongoDbOptions.Value.ApplicationName
		});
	}

	async public Task SnapshotAsync(T aggregate, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(aggregate, nameof(aggregate));

		if (await _mongoDbClient.UpsertAsync(aggregate, BuildPredicate(aggregate), cancellationToken))
			_telemetry.SnapshotCreated(_aggregateName);
	}

	static FilterDefinition<T> BuildPredicate(T aggregate)
	{
		var predicate = new FilterDefinitionBuilder<T>()
			.Eq(MongoDbAggregateSerializer<T>.BsonDocuemntIdPropertyName, aggregate.Id());

		return predicate;
	}
}
