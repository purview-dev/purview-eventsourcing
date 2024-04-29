﻿using Microsoft.Extensions.Caching.Distributed;
using NSubstitute.ReturnsExtensions;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.AzureStorage.Table.StorageClients.Blob;
using Purview.EventSourcing.AzureStorage.Table.StorageClients.Table;
using Purview.EventSourcing.ChangeFeed;
using Purview.EventSourcing.Services;

namespace Purview.EventSourcing.AzureStorage.Table;

public sealed class TableEventStoreFixture : IAsyncLifetime
{
	readonly Testcontainers.Azurite.AzuriteContainer _azuriteContainer;

	string _containerName = default!;
	string _tableName = default!;

	IDistributedCache _distributedCache = default!;
	ITableEventStoreTelemetry _telemetry = default!;
	IAggregateEventNameMapper _eventNameMapper = default!;

	AzureTableClient _tableClient = default!;
	AzureBlobClient _blobClient = default!;

	IDisposable? _eventStoreAsDisposable;

	public TableEventStoreFixture()
	{
		_azuriteContainer = ContainerHelper.CreateAzurite();
	}

	public IDistributedCache Cache => _distributedCache;

	public ITableEventStoreTelemetry Telemetry => _telemetry;

	internal AzureTableClient TableClient => _tableClient;

	internal AzureBlobClient BlobClient => _blobClient;

	public TableEventStore<TAggregate> CreateEventStore<TAggregate>(
		IAggregateChangeFeedNotifier<TAggregate>? aggregateChangeNotifier = null,
		int correlationIdsToGenerate = 1,
		bool removeFromCacheOnDelete = false,
		int snapshotRecalculationInterval = 1)
		where TAggregate : class, IAggregate, new()
	{
		var runId = Guid.NewGuid();
		var runIds = Enumerable.Range(1, correlationIdsToGenerate)
			.Select(_ => $"{Guid.NewGuid()}".ToUpperInvariant())
			.ToArray();

		_tableName = TestHelpers.GenAzureTableName(runId);
		_containerName = TestHelpers.GenAzureBlobContainerName(runId);

		_distributedCache = CreateDistributedCache();
		_telemetry = Substitute.For<ITableEventStoreTelemetry>();
		_eventNameMapper = new AggregateEventNameMapper();

		var aggregateRequirementsManager = Substitute.For<IAggregateRequirementsManager>();

		var azureStorageOptions = new Options.AzureStorageEventStoreOptions
		{
			ConnectionString = _azuriteContainer.GetConnectionString(),
			Table = _tableName,
			Container = _containerName,
			TimeoutInSeconds = 10,
			RemoveDeletedFromCache = removeFromCacheOnDelete,
			SnapshotInterval = snapshotRecalculationInterval
		};

		TableEventStore<TAggregate> eventStore = new(
			eventNameMapper: _eventNameMapper,
			azureStorageOptions: Microsoft.Extensions.Options.Options.Create(azureStorageOptions),
			distributedCache: _distributedCache,
			aggregateChangeNotifier: aggregateChangeNotifier ?? Substitute.For<IAggregateChangeFeedNotifier<TAggregate>>(),
			eventStoreTelemetry: _telemetry,
			aggregateRequirementsManager: aggregateRequirementsManager
		);

		_tableClient = new(azureStorageOptions, eventStore.TableName);
		_blobClient = new(azureStorageOptions, eventStore.ContainerName);

		_eventStoreAsDisposable = eventStore as IDisposable;

		return eventStore;
	}

	public static IDistributedCache CreateDistributedCache()
	{
		var cache = Substitute.For<IDistributedCache>();
		cache
			.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.ReturnsNullForAnyArgs();

		return cache;
	}

	public Task InitializeAsync() => _azuriteContainer.StartAsync();

	public async Task DisposeAsync()
	{
		_eventStoreAsDisposable?.Dispose();

		await _azuriteContainer.DisposeAsync();
	}
}
