using Microsoft.Extensions.Caching.Distributed;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.AzureStorage.Table;
using Purview.EventSourcing.AzureStorage.Table.StorageClients.Blob;
using Purview.EventSourcing.AzureStorage.Table.StorageClients.Table;
using Purview.EventSourcing.ChangeFeed;
using Purview.EventSourcing.MongoDb;
using Purview.EventSourcing.MongoDb.Snapshot;
using Purview.EventSourcing.Services;

namespace Purview.EventSourcing.SnapshotOnly.MongoDb;

sealed public class MongoDbSnapshotTestContext
{
	readonly string _mongoDbConnectionString;
	readonly string _azuriteConnectionString;

	ITableEventStoreTelemetry _telemetry = default!;
	IAggregateEventNameMapper _eventNameMapper = default!;
	IAggregateChangeFeedNotifier<PersistenceAggregate> _aggregateChangeNotifier = default!;

	MongoDbClient _mongoDbClient = default!;
	AzureTableClient _tableClient = default!;
	AzureBlobClient _blobClient = default!;

	public Guid RunId { get; } = Guid.NewGuid();

	internal MongoDbClient MongoDbClient => _mongoDbClient;

	internal AzureTableClient TableClient => _tableClient;

	internal AzureBlobClient BlobClient => _blobClient;

	public MongoDbSnapshotEventStore<PersistenceAggregate> EventStore { get; init; }

	public MongoDbSnapshotTestContext(string mongoDbConnectionString, string azuriteConnectionString, int correlationIdsToGenerate = 1, string? collectionName = null)
	{
		_mongoDbConnectionString = mongoDbConnectionString;
		_azuriteConnectionString = azuriteConnectionString;

		EventStore = CreateMongoDbEventStore(correlationIdsToGenerate, collectionName);
	}

	public MongoDbSnapshotEventStore<PersistenceAggregate> CreateMongoDbEventStore(int correlationIdsToGenerate = 1, string? collectionName = null)
	{
		var tableEventStore = CreateTableEventStore(correlationIdsToGenerate);

		MongoDbEventStoreOptions config = new()
		{
			ConnectionString = _mongoDbConnectionString,
			Database = GetType().Name,
			Collection = collectionName ?? TestHelpers.GenMongoDbCollectionName()
		};

		MongoDbSnapshotEventStore<PersistenceAggregate> eventStore = new(
			tableEventStore,
			Microsoft.Extensions.Options.Options.Create(config),
			Substitute.For<IMongoDbSnapshotEventStoreTelemetry>()
		);

		_mongoDbClient = new(new()
		{
			ConnectionString = config.ConnectionString,
			ApplicationName = "purview-integration-tests",
			Database = config.Database,
			Collection = config.Collection
		});

		return eventStore;
	}

	TableEventStore<PersistenceAggregate> CreateTableEventStore(int correlationIdsToGenerate = 1)
	{
		var runIds = Enumerable.Range(1, correlationIdsToGenerate).Select(_ => $"{Guid.NewGuid()}".ToUpperInvariant()).ToArray();

		_eventNameMapper = new AggregateEventNameMapper();
		_telemetry = Substitute.For<ITableEventStoreTelemetry>();
		_aggregateChangeNotifier = Substitute.For<IAggregateChangeFeedNotifier<PersistenceAggregate>>();

		AzureStorage.Table.Options.AzureStorageEventStoreOptions azureStorageOptions = new()
		{
			ConnectionString = _azuriteConnectionString,
			Table = TestHelpers.GenAzureTableName(RunId),
			Container = TestHelpers.GenAzureBlobContainerName(RunId),
			TimeoutInSeconds = 10,
			RemoveDeletedFromCache = true,
			SnapshotInterval = 1
		};

		TableEventStore<PersistenceAggregate> eventStore = new(
			eventNameMapper: _eventNameMapper,
			azureStorageOptions: Microsoft.Extensions.Options.Options.Create(azureStorageOptions),
			distributedCache: Substitute.For<IDistributedCache>(),
			aggregateChangeNotifier: Substitute.For<IAggregateChangeFeedNotifier<PersistenceAggregate>>(),
			eventStoreTelemetry: _telemetry,
			aggregateRequirementsManager: Substitute.For<IAggregateRequirementsManager>()
		);

		_tableClient = new(azureStorageOptions, eventStore.TableName);
		_blobClient = new(azureStorageOptions, eventStore.ContainerName);

		return eventStore;
	}
}
