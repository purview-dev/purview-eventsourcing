using Microsoft.Extensions.Caching.Distributed;
using NSubstitute.ReturnsExtensions;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.ChangeFeed;
using Purview.EventSourcing.MongoDb.StorageClients;
using Purview.EventSourcing.Services;

namespace Purview.EventSourcing.MongoDb;

public sealed class MongoDbEventStoreFixture : IAsyncLifetime
{
	readonly Testcontainers.MongoDb.MongoDbContainer _mongoDbContainer;

	IAggregateEventNameMapper _eventNameMapper = default!;
	IDisposable? _eventStoreAsDisposable;

	public MongoDbEventStoreFixture()
	{
		_mongoDbContainer = ContainerHelper.CreateMongoDb();
	}

	public IDistributedCache Cache { get; private set; } = default!;

	public IMongoDbEventStoreTelemetry Telemetry { get; private set; } = default!;

	internal MongoDbClient MongoDbClient { get; private set; } = default!;

	public MongoDbEventStore<TAggregate> CreateEventStore<TAggregate>(
		IAggregateChangeFeedNotifier<TAggregate>? aggregateChangeNotifier = null,
		int correlationIdsToGenerate = 1,
		bool removeFromCacheOnDelete = false)
		where TAggregate : class, IAggregate, new()
	{
		var runId = Guid.NewGuid();
		var runIds = Enumerable.Range(1, correlationIdsToGenerate)
			.Select(_ => $"{Guid.NewGuid()}".ToUpperInvariant())
			.ToArray();


		Cache = CreateDistributedCache();
		Telemetry = Substitute.For<IMongoDbEventStoreTelemetry>();

		_eventNameMapper = new AggregateEventNameMapper();

		var aggregateRequirementsManager = Substitute.For<IAggregateRequirementsManager>();
		MongoDbEventStoreOptions mongoDbOptions = new()
		{
			ConnectionString = _mongoDbContainer.GetConnectionString(),
			Database = $"TestDatabase_{runId}",
			Collection = $"TestCollection_{runId}",
			TimeoutInSeconds = 10,
			RemoveDeletedFromCache = removeFromCacheOnDelete,
		};

		MongoDbEventStore<TAggregate> eventStore = new(
			eventNameMapper: _eventNameMapper,
			mongoDbOptions: Microsoft.Extensions.Options.Options.Create(mongoDbOptions),
			distributedCache: Cache,
			aggregateChangeNotifier: aggregateChangeNotifier ?? Substitute.For<IAggregateChangeFeedNotifier<TAggregate>>(),
			eventStoreTelemetry: Telemetry,
			aggregateRequirementsManager: aggregateRequirementsManager
		);

		MongoDbClient = new(mongoDbOptions, mongoDbOptions.Database, mongoDbOptions.Collection);

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

	public Task InitializeAsync() => _mongoDbContainer.StartAsync();

	public async Task DisposeAsync()
	{
		_eventStoreAsDisposable?.Dispose();

		await _mongoDbContainer.DisposeAsync();
	}
}
