using Microsoft.Extensions.Caching.Distributed;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.AzureStorage.Table;
using Purview.EventSourcing.AzureStorage.Table.StorageClients.Blob;
using Purview.EventSourcing.AzureStorage.Table.StorageClients.Table;
using Purview.EventSourcing.ChangeFeed;
using Purview.EventSourcing.CosmosDb;
using Purview.EventSourcing.CosmosDb.Snapshot;
using Purview.EventSourcing.Services;

namespace Purview.EventSourcing.SnapshotOnly.CosmosDb;

public sealed class CosmosDbSnapshotEventStoreContext(string cosmosDbConnectionString, HttpClient cosmosDbHttpClient, string azuriteConnectionString) : IAsyncDisposable
{
	ITableEventStoreTelemetry _telemetry = default!;
	IAggregateEventNameMapper _eventNameMapper = default!;
	IAggregateChangeFeedNotifier<PersistenceAggregate> _aggregateChangeNotifier = default!;

	CosmosDbSnapshotEventStore<PersistenceAggregate> _eventStore = default!;

	CosmosDbClient _cosmosDbClient = default!;
	AzureTableClient _tableClient = default!;
	AzureBlobClient _blobClient = default!;

	public Guid RunId { get; } = Guid.NewGuid();

	public CosmosDbSnapshotEventStore<PersistenceAggregate> EventStore => _eventStore;

	internal AzureTableClient TableClient => _tableClient;

	internal AzureBlobClient BlobClient => _blobClient;

	internal CosmosDbClient CosmosDbClient => _cosmosDbClient;

	public void CreateCosmosDbEventStore(int correlationIdsToGenerate = 1, string? container = null)
	{
		var tableEventStore = CreateTableEventStore(correlationIdsToGenerate);

		CosmosDbEventStoreOptions config = new()
		{
			Container = container ?? TestHelpers.GenAzureCosmosDbContainerName(),
			ConnectionString = cosmosDbConnectionString,
			Database = GetType().Name,
			RequestTimeoutInSeconds = 30,
			IgnoreSSLWarnings = true,
			ConnectionMode = Microsoft.Azure.Cosmos.ConnectionMode.Gateway
		};

		Microsoft.Azure.Cosmos.CosmosClient cosmosClient = new(config.ConnectionString, clientOptions: new Microsoft.Azure.Cosmos.CosmosClientOptions()
		{
			HttpClientFactory = () => cosmosDbHttpClient,
			ConnectionMode = config.ConnectionMode,
			LimitToEndpoint = true
		});

		_cosmosDbClient = new(
			config,
			$"/{nameof(IAggregate.AggregateType)}",
			null,
			cosmosClient
		);

		CosmosDbSnapshotEventStore<PersistenceAggregate> eventStore = new(
			tableEventStore,
			Microsoft.Extensions.Options.Options.Create(config),
			cosmosClient
		);

		_eventStore = eventStore;
	}

	TableEventStore<PersistenceAggregate> CreateTableEventStore(int correlationIdsToGenerate = 1)
	{
		var runIds = Enumerable.Range(1, correlationIdsToGenerate).Select(_ => $"{Guid.NewGuid()}".ToUpperInvariant()).ToArray();

		_eventNameMapper = new AggregateEventNameMapper();
		_telemetry = Substitute.For<ITableEventStoreTelemetry>();
		_aggregateChangeNotifier = Substitute.For<IAggregateChangeFeedNotifier<PersistenceAggregate>>();

		AzureStorage.Table.Options.AzureStorageEventStoreOptions azureStorageOptions = new()
		{
			ConnectionString = azuriteConnectionString,
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

	public async ValueTask DisposeAsync()
		=> await _cosmosDbClient.DeleteContainerAsync();
}
