using Microsoft.Extensions.Caching.Distributed;
using NSubstitute.ReturnsExtensions;
using Purview.EventSourcing.Interfaces.Aggregates;
using Purview.EventSourcing.Interfaces.AzureStorage.Table;
using Purview.EventSourcing.Interfaces.ChangeFeed;
using Purview.EventSourcing.Interfaces.Services;
using Purview.EventSourcing.Services;
using Purview.Interfaces.Identity;
using Purview.Interfaces.Storage;
using Purview.Interfaces.Storage.AzureStorage.Blob;
using Purview.Interfaces.Storage.AzureStorage.Table;
using Purview.Interfaces.Tracking;
using Purview.Storage.AzureStorage.Blob;
using Purview.Storage.AzureStorage.Table;
using Purview.Testing;

namespace Purview.EventSourcing.AzureStorage.Table;

public sealed class TableEventStoreFixture : IAsyncLifetime
{
	readonly Testcontainers.Azurite.AzuriteContainer _azuriteContainer;

	string _containerName = default!;
	string _tableName = default!;

	IDistributedCache _cache = default!;
	ITableClient _tableClient = default!;
	IBlobClient _blobClient = default!;
	ICorrelationIdProvider _correlationIdProvider = default!;
	ITableEventStoreTelemetry _logs = default!;
	IAggregateEventNameMapper _eventNameMapper = default!;

	IDisposable? _eventStoreAsDisposable;

	public TableEventStoreFixture()
	{
		_azuriteContainer = ContainerHelper.CreateAzurite();
	}

	public IDistributedCache Cache => _cache;

	public IBlobClient BlobClient => _blobClient;

	public ITableClient TableClient => _tableClient;

	public ITableEventStoreTelemetry Logs => _logs;

	public TableEventStore<TAggregate> CreateEventStore<TAggregate>(
		IAggregateChangeFeedNotifier<TAggregate>? aggregateChangeNotifier = null,
		int correlationIdsToGenerate = 1,
		bool removeFromCacheOnDelete = false,
		int snapshotRecalculationInterval = 1)
		where TAggregate : class, IAggregate, new()
	{
		Guid runId = Guid.NewGuid();
		string[] runIds = Enumerable.Range(1, correlationIdsToGenerate).Select(_ => Guid.NewGuid().ToLoweredString()).ToArray();

		_tableName = TestDataHelper.GenAzureTableName(runId);
		_containerName = TestDataHelper.GenAzureBlobContainerName(runId);

		_correlationIdProvider = Substitute.For<ICorrelationIdProvider>();
		_correlationIdProvider
			.GetCorrelationId()
			.Returns(runId.ToLoweredString(), runIds);

		_cache = CreateDistributedCache();
		_logs = Substitute.For<ITableEventStoreTelemetry>();
		_eventNameMapper = new AggregateEventNameMapper();

		IAggregateRequirementsManager aggregateRequirementsManager = Substitute.For<IAggregateRequirementsManager>();

		TableEventStore<TAggregate> eventStore = new(
			eventNameMapper: _eventNameMapper,
			storageClientFactory: CreateStorageFactory(),
			tableConfiguration: new Options.TableEventStoreConfiguration
			{
				ConnectionString = _azuriteContainer.GetConnectionString(),
				Table = _tableName,
				Container = _containerName,
				TimeoutInSeconds = 10,
				RemoveDeletedFromCache = removeFromCacheOnDelete,
				SnapshotInterval = snapshotRecalculationInterval
			}.AsOptions(),
			blobConfigurationFunc: null!,
			cache: _cache,
			principalService: CreatePrincipalService(),
			correlationIdProvider: _correlationIdProvider,
			aggregateChangeNotifier: aggregateChangeNotifier ?? Substitute.For<IAggregateChangeFeedNotifier<TAggregate>>(),
			eventStoreLog: _logs,
			aggregateRequirementsManager: aggregateRequirementsManager
		);

		_eventStoreAsDisposable = eventStore as IDisposable;

		return eventStore;
	}

	public static IPrincipalService CreatePrincipalService() => Substitute.For<IPrincipalService>();

	public static IDistributedCache CreateDistributedCache()
	{
		IDistributedCache cache = Substitute.For<IDistributedCache>();
		cache
			.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.ReturnsNullForAnyArgs();

		return cache;
	}

	IStorageClientFactory CreateStorageFactory()
	{
		_blobClient = new BlobClient(new Options.BlobEventStoreConfiguration
		{
			ConnectionString = _azuriteContainer.GetConnectionString(),
			Container = _containerName,
			TimeoutInSeconds = 10
		});

		_tableClient = new TableClient(new Options.TableEventStoreConfiguration
		{
			ConnectionString = _azuriteContainer.GetConnectionString(),
			Table = _tableName,
			TimeoutInSeconds = 10
		}, null);

		IStorageClientFactory factory = Substitute.For<IStorageClientFactory>();

		factory
			.GetOrBuild<ITableClient, Options.TableEventStoreConfiguration>(Arg.Any<string>(), Arg.Any<Options.TableEventStoreConfiguration>(), Arg.Any<IDictionary<string, object?>?>())
			.Returns(_tableClient);

		factory
			.GetOrBuild<IBlobClient, Options.BlobEventStoreConfiguration>(Arg.Any<string>(), Arg.Any<Options.BlobEventStoreConfiguration>(), Arg.Any<IDictionary<string, object?>?>())
			.Returns(_blobClient);

		return factory;
	}

	public Task InitializeAsync() => _azuriteContainer.StartAsync();

	public async Task DisposeAsync()
	{
		_eventStoreAsDisposable?.Dispose();

		await _azuriteContainer.DisposeAsync();
	}
}
