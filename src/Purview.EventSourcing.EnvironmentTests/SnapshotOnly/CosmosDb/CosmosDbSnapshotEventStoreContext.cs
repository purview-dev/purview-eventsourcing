using Microsoft.Extensions.Caching.Distributed;
using NSubstitute.ReturnsExtensions;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.AzureStorage.Table;
using Purview.EventSourcing.AzureStorage.Table.Options;
using Purview.EventSourcing.CosmosDb.Snapshot;
using Purview.EventSourcing.CosmosDb.Snapshot.Options;
using Purview.EventSourcing.Interfaces.Aggregates;
using Purview.EventSourcing.Interfaces.AzureStorage.Table;
using Purview.EventSourcing.Interfaces.ChangeFeed;
using Purview.EventSourcing.Interfaces.Services;
using Purview.EventSourcing.Services;
using Purview.Interfaces.Identity;
using Purview.Interfaces.Storage;
using Purview.Interfaces.Storage.AzureStorage.Blob;
using Purview.Interfaces.Storage.AzureStorage.Table;
using Purview.Interfaces.Storage.CosmosDb;
using Purview.Interfaces.Tracking;
using Purview.Options.Storage.AzureStorage;
using Purview.Storage.AzureStorage.Blob;
using Purview.Storage.AzureStorage.Table;
using Purview.Storage.CosmosDb;
using Purview.Testing;

namespace Purview.EventSourcing.SnapshotOnly.CosmosDb;

public sealed class CosmosDbSnapshotEventStoreContext
	(string cosmosDbConnectionString, HttpClient cosmosDbHttpClient, string azuriteConnectionString)
	: IAsyncDisposable
{
	const int StorageOperationTimeout = 30;

	Guid? _userId;

	string? _tableName;
	string? _blobContainerName;
	string? _cosmosDbContainerName;

	ITableClient _tableClient = default!;
	IBlobClient _blobClient = default!;
	CosmosDbClient _cosmosDbClient = default!;

	ICorrelationIdProvider _correlationIdProvider = default!;
	ITableEventStoreTelemetry _logs = default!;
	IStorageClientFactory _storageFactory = default!;
	IAggregateEventNameMapper _eventNameMapper = default!;
	IAggregateChangeFeedNotifier<PersistenceAggregate> _aggregateChangeNotifier = default!;

	CosmosDbEventStoreConfiguration? _cosmosDbConfig;
	CosmosDbSnapshotEventStore<PersistenceAggregate> _eventStore = default!;

	public Guid RunId { get; } = Guid.NewGuid();

	public CosmosDbClient CosmosDbClient => _cosmosDbClient;

	public CosmosDbSnapshotEventStore<PersistenceAggregate> EventStore => _eventStore;

	public void CreateCosmosDbEventStore(int correlationIdsToGenerate = 1)
	{
		_storageFactory = CreateStorageFactory();

		var tableEventStore = CreateTableEventStore(correlationIdsToGenerate);

		CosmosDbSnapshotEventStore<PersistenceAggregate> eventStore = new(
			tableEventStore,
			_cosmosDbConfig!.AsOptionsSnapshot(),
			_storageFactory
		);

		_eventStore = eventStore;
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
				ConnectionString = azuriteConnectionString,
				Table = _tableName!,
				Container = _blobContainerName,
			}.AsOptionsSnapshot(),
			blobConfigurationFunc: null!,
			cache: CreateDistributedCache(),
			principalService: CreatePrincipalService(),
			correlationIdProvider: _correlationIdProvider,
			aggregateChangeNotifier: _aggregateChangeNotifier,
			eventStoreLog: _logs,
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

	IStorageClientFactory CreateStorageFactory()
	{
		_tableName = TestDataHelper.GenAzureTableName(RunId);
		_blobContainerName = TestDataHelper.GenAzureBlobContainerName(RunId);
		_cosmosDbContainerName = TestDataHelper.GenAzureCosmosDbContainerName(RunId);

		_blobClient = new BlobClient(CreateBlobConfig(azuriteConnectionString, _blobContainerName, StorageOperationTimeout));
		_tableClient = new TableClient(CreateTableConfig(azuriteConnectionString, _tableName, StorageOperationTimeout), null);

		(var client, var config) = CreateCosmosDbClient(_cosmosDbContainerName);

		_cosmosDbClient = client;
		_cosmosDbConfig = config;

		var factory = Substitute.For<IStorageClientFactory>();

		factory
			.GetOrBuild<ITableClient, TableEventStoreConfiguration>(Arg.Any<string>(), Arg.Any<TableEventStoreConfiguration>(), Arg.Any<IDictionary<string, object?>?>())
			.Returns(_tableClient);

		factory
			.GetOrBuild<IBlobClient, BlobEventStoreConfiguration>(Arg.Any<string>(), Arg.Any<BlobEventStoreConfiguration>(), Arg.Any<IDictionary<string, object?>?>())
			.Returns(_blobClient);

		factory
			.GetOrBuild<ICosmosDbClient, CosmosDbEventStoreConfiguration>(Arg.Any<string>(), Arg.Any<CosmosDbEventStoreConfiguration>(), Arg.Any<IDictionary<string, object?>?>())
			.Returns(client);

		return factory;
	}

	CosmosDbEventStoreConfiguration CreateCosmosDbConfig(string? container)
	{
		CosmosDbEventStoreConfiguration config = new();

		container = container.OrDefault(() => TestDataHelper.GenAzureCosmosDbContainerName());

		config.ConnectionString = cosmosDbConnectionString;
		config.Database = GetType().Name;
		config.Container = container;
		config.RequestTimeoutInSeconds = 30;
		config.IgnoreSSLWarnings = true;
		config.ConnectionMode = Microsoft.Azure.Cosmos.ConnectionMode.Gateway;

		return config;
	}

	(CosmosDbClient client, CosmosDbEventStoreConfiguration config) CreateCosmosDbClient(string? container = null)
	{
		var config = CreateCosmosDbConfig(container);
		Microsoft.Azure.Cosmos.CosmosClient cosmosDbClient = new(config.ConnectionString, clientOptions: new Microsoft.Azure.Cosmos.CosmosClientOptions()
		{
			HttpClientFactory = () => cosmosDbHttpClient,
			ConnectionMode = config.ConnectionMode,
			LimitToEndpoint = true
		});

		CosmosDbClient client = new(
			config,
			$"/{nameof(IAggregate.AggregateType)}",
			null,
			cosmosDbClient);

		return (client, config);
	}

	IPrincipalService CreatePrincipalService()
	{
		_userId = Guid.NewGuid();

		var principalService = Substitute.For<IPrincipalService>();

		principalService
			.Identifier()
			.Returns(_userId.Value.ToString());

		return principalService;
	}

	public async ValueTask DisposeAsync() => await _cosmosDbClient.DeleteContainerAsync();
}
