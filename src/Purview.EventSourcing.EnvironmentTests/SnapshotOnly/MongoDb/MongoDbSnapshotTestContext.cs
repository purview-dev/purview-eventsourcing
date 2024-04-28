using Microsoft.Extensions.Caching.Distributed;
using NSubstitute.ReturnsExtensions;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.AzureStorage.Table;
using Purview.EventSourcing.AzureStorage.Table.Options;
using Purview.EventSourcing.Interfaces.AzureStorage.Table;
using Purview.EventSourcing.Interfaces.ChangeFeed;
using Purview.EventSourcing.Interfaces.Services;
using Purview.EventSourcing.MongoDb.Snapshot;
using Purview.EventSourcing.MongoDb.Snapshot.Options;
using Purview.EventSourcing.Services;
using Purview.Interfaces.Identity;
using Purview.Interfaces.Storage;
using Purview.Interfaces.Storage.AzureStorage.Blob;
using Purview.Interfaces.Storage.AzureStorage.Table;
using Purview.Interfaces.Storage.MongoDb;
using Purview.Interfaces.Tracking;
using Purview.Options.Storage;
using Purview.Options.Storage.AzureStorage;
using Purview.Storage.AzureStorage.Blob;
using Purview.Storage.AzureStorage.Table;
using Purview.Storage.MongoDb;
using Purview.Testing;

namespace Purview.EventSourcing.SnapshotOnly.MongoDb;

public class MongoDbSnapshotTestContext : IDisposable
{
	const int AzureStorageOperationTimeoutInSeconds = 10;

	readonly string _mongoDbConnectionString;
	readonly string _azuriteConnectionString;

	string? _tableName;
	string? _containerName;
	string? _collectionName;
	string? _databaseName;

	BlobClient _blobClient = default!;
	TableClient _tableClient = default!;
	MongoDbClient _mongoDbClient = default!;

	Guid? _userId;

	IAggregateEventNameMapper _eventNameMapper = default!;
	ICorrelationIdProvider _correlationIdProvider = default!;
	ITableEventStoreTelemetry _logs = default!;
	IAggregateChangeFeedNotifier<PersistenceAggregate> _aggregateChangeNotifier = default!;
	IStorageClientFactory _storageFactory = default!;

	public Guid RunId { get; } = Guid.NewGuid();

	public MongoDbClient MongoDbClient => _mongoDbClient;

	public MongoDbSnapshotEventStore<PersistenceAggregate> EventStore { get; init; }

	public MongoDbSnapshotTestContext(string mongoDbConnectionString, string azuriteConnectionString, int correlationIdsToGenerate = 1, string? collectionName = null)
	{
		_mongoDbConnectionString = mongoDbConnectionString;
		_azuriteConnectionString = azuriteConnectionString;

		EventStore = CreateMongoDbEventStore(correlationIdsToGenerate, collectionName);
	}

	public MongoDbSnapshotEventStore<PersistenceAggregate> CreateMongoDbEventStore(int correlationIdsToGenerate = 1, string? collectionName = null)
	{
		_storageFactory = CreateStorageFactory(collectionName);

		var tableEventStore = CreateTableEventStore(correlationIdsToGenerate);

		MongoDbSnapshotEventStore<PersistenceAggregate> eventStore = new(
			tableEventStore,
			new MongoDbEventStoreConfiguration
			{
				ConnectionString = _mongoDbConnectionString,
				Database = _databaseName!,
				Collection = collectionName!
			}.AsOptionsSnapshot(),
			_storageFactory
		);

		return eventStore;
	}

	TableEventStore<PersistenceAggregate> CreateTableEventStore(int correlationIdsToGenerate = 1)
	{
		var runIds = Enumerable.Range(1, correlationIdsToGenerate).Select(_ => Guid.NewGuid().ToLoweredString()).ToArray();

		_eventNameMapper = new AggregateEventNameMapper();

		_correlationIdProvider = Substitute.For<ICorrelationIdProvider>();
		_correlationIdProvider
			.GetCorrelationId()
			.Returns(RunId.ToLoweredString(), runIds);

		_logs = Substitute.For<ITableEventStoreTelemetry>();
		_aggregateChangeNotifier = Substitute.For<IAggregateChangeFeedNotifier<PersistenceAggregate>>();

		var aggregateRequirementsManager = Substitute.For<IAggregateRequirementsManager>();

		TableEventStore<PersistenceAggregate> eventStore = new(
			eventNameMapper: _eventNameMapper,
			storageClientFactory: _storageFactory,
			tableConfiguration: new TableEventStoreConfiguration
			{
				ConnectionString = _azuriteConnectionString,
				Table = _tableName!,
				Container = _containerName,
			}.AsOptionsSnapshot(),
			blobConfigurationFunc: null!,
			cache: CreateDistributedCache(),
			principalService: CreatePrincipalService(),
			correlationIdProvider: _correlationIdProvider,
			eventStoreLog: _logs,
			aggregateChangeNotifier: _aggregateChangeNotifier,
			aggregateRequirementsManager: aggregateRequirementsManager
		);

		return eventStore;
	}

	static IDistributedCache CreateDistributedCache()
	{
		var cache = Substitute.For<IDistributedCache>();

		cache
			.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.ReturnsNullForAnyArgs();

		return cache;
	}

	static BlobConfiguration CreateBlobConfig(string connectionString, string container, int timeout)
	{
		BlobConfiguration config = new()
		{
			ConnectionString = connectionString,
			Container = container,
			TimeoutInSeconds = timeout
		};

		return config;
	}

	static TableConfiguration CreateTableConfig(string connectionString, string tableName, int timeout)
	{
		TableConfiguration config = new()
		{
			ConnectionString = connectionString,
			Table = tableName,
			TimeoutInSeconds = timeout
		};

		return config;
	}

	static MongoDbConfiguration CreateMongoDbConfig(string connectionString, string database, string collection)
	{
		MongoDbConfiguration config = new()
		{
			ConnectionString = connectionString,
			Database = database,
			Collection = collection
		};

		return config;
	}

	IStorageClientFactory CreateStorageFactory(string? collectionName = null)
	{
		_tableName = TestDataHelper.GenAzureTableName(RunId);
		_containerName = TestDataHelper.GenAzureBlobContainerName(RunId);
		_collectionName = collectionName.OrDefault(() => TestDataHelper.GenMongoDbCollectionName(RunId));
		_databaseName = TestDataHelper.GenMongoDbCollectionName(RunId, "d_");

		_blobClient = new BlobClient(CreateBlobConfig(_azuriteConnectionString, _containerName, AzureStorageOperationTimeoutInSeconds));
		_tableClient = new TableClient(CreateTableConfig(_azuriteConnectionString, _tableName, AzureStorageOperationTimeoutInSeconds), null);
		_mongoDbClient = new MongoDbClient(CreateMongoDbConfig(_mongoDbConnectionString, _databaseName, _collectionName), null);

		var factory = Substitute.For<IStorageClientFactory>();

		factory
			.GetOrBuild<ITableClient, TableEventStoreConfiguration>(Arg.Any<string>(), Arg.Any<TableEventStoreConfiguration>(), Arg.Any<IDictionary<string, object?>?>())
			.Returns(_tableClient);

		factory
			.GetOrBuild<IBlobClient, BlobEventStoreConfiguration>(Arg.Any<string>(), Arg.Any<BlobEventStoreConfiguration>(), Arg.Any<IDictionary<string, object?>?>())
			.Returns(_blobClient);

		factory
			.Build<IMongoDbClient, MongoDbEventStoreConfiguration>(Arg.Any<MongoDbEventStoreConfiguration>(), Arg.Any<IDictionary<string, object?>?>())
			.Returns(_mongoDbClient);

		return factory;
	}

	IPrincipalService CreatePrincipalService()
	{
		_userId = Guid.NewGuid();

		var principalService = Substitute.For<IPrincipalService>();

		principalService
			.Identifier()
			.Returns($"{_userId.Value}");

		return principalService;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_mongoDbClient?.Dispose();
		}
	}
}
