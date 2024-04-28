using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Interfaces;
using Purview.EventSourcing.Interfaces.MongoDb;
using Purview.EventSourcing.MongoDb.Snapshot.Options;
using Purview.Interfaces.Storage;
using Purview.Interfaces.Storage.MongoDb;

namespace Purview.EventSourcing.MongoDb.Snapshot;

public partial class MongoDbSnapshotEventStore<T> : IMongoDbSnapshotEventStore<T>
	where T : AggregateBase, new()
{
	readonly IEventStore<T> _eventStore;
	readonly Lazy<IMongoDbClient> _mongoDbClient;
	readonly IStorageClientFactory _storageFactory;
	readonly IOptions<MongoDbEventStoreConfiguration> _configuration;

	static MongoDbSnapshotEventStore()
	{
		BsonSerializer.RegisterSerializationProvider(new MongoDbAggregateSerializationProvider());
	}

	public MongoDbSnapshotEventStore(
		Interfaces.Internal.INonQueryableEventStore<T> eventStore,
		IOptions<MongoDbEventStoreConfiguration> configuration,
		IStorageClientFactory storageFactory)
	{
		_eventStore = eventStore;
		_configuration = configuration;
		_storageFactory = storageFactory;

		var collectionName = $"{TypeNameHelper.GetName(typeof(T), "aggregate")}-store";
		_mongoDbClient = new Lazy<IMongoDbClient>(() =>
			_storageFactory.Build<IMongoDbClient, MongoDbEventStoreConfiguration>(_configuration.Value, new Dictionary<string, object?> { { "collection", collectionName } }));
	}

	public Task ForceSaveAsync(T aggregate, CancellationToken cancellationToken = default)
	{
		if (aggregate == null)
		{
			throw new ArgumentNullException(nameof(aggregate));
		}

		return _mongoDbClient.Value.UpsertAsync(aggregate, BuildPredicate(aggregate), cancellationToken);
	}

	static FilterDefinition<T> BuildPredicate(T aggregate)
	{
		var predicate = new FilterDefinitionBuilder<T>()
			.Eq(MongoDbAggregateSerializer<T>.BsonDocuemntIdPropertyName, aggregate.Id());

		return predicate;
	}
}
