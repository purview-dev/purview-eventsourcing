using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Services;

namespace Purview.EventSourcing.MongoDb;

public sealed partial class MongoDbEventStore<T> : IMongoDbEventStore<T>
	where T : class, IAggregate, new()
{
	const int _serializationBufferSize = 4096;
	const int _maxEventSize = 32000;

	readonly Purview.EventSourcing.MongoDb.StorageClients.MongoDbClient _client;

	readonly IAggregateEventNameMapper _eventNameMapper;
	readonly IOptions<MongoDbEventStoreOptions> _eventStoreOptions;
	readonly FluentValidation.IValidator<T>? _validator;
	readonly IAggregateIdFactory? _aggregateIdFactory;
	readonly IDistributedCache _distributedCache;
	readonly IMongoDbEventStoreTelemetry _eventStoreTelemetry;
	readonly ChangeFeed.IAggregateChangeFeedNotifier<T> _aggregateChangeNotifier;
	readonly IAggregateRequirementsManager _aggregateRequirementsManager;

	readonly string _aggregateTypeFullName;
	readonly string _aggregateTypeShortName;

	public MongoDbEventStore(
		IAggregateEventNameMapper eventNameMapper,
		[NotNull] IOptions<MongoDbEventStoreOptions> mongoDBOptions,
		IDistributedCache distributedCache,
		IMongoDbEventStoreTelemetry eventStoreTelemetry,
		ChangeFeed.IAggregateChangeFeedNotifier<T> aggregateChangeNotifier,
		IAggregateRequirementsManager aggregateRequirementsManager,
		FluentValidation.IValidator<T>? validator = null,
		IAggregateIdFactory? aggregateIdFactory = null)
	{
		_eventNameMapper = eventNameMapper;
		_eventStoreOptions = mongoDBOptions;
		_validator = validator;
		_aggregateIdFactory = aggregateIdFactory;
		_distributedCache = distributedCache;
		_eventStoreTelemetry = eventStoreTelemetry;
		_aggregateChangeNotifier = aggregateChangeNotifier;
		_aggregateRequirementsManager = aggregateRequirementsManager;

		var name = typeof(T).Name;

		_client = new(new StorageClients.MongoDbConfiguration
		{
			ApplicationName = mongoDBOptions.Value.ApplicationName,
			Database = mongoDBOptions.Value.Database,
			Collection = mongoDBOptions.Value.Collection ?? "es-" + name,
			ConnectionString = mongoDBOptions.Value.ConnectionString
		});


		_aggregateTypeShortName = typeof(T).Name;
		_aggregateTypeFullName = typeof(T).FullName ?? _aggregateTypeShortName;

		var aggregateName = _eventNameMapper.InitializeAggregate<T>();
		if (!aggregateName.Contains('.', StringComparison.InvariantCulture))
			// Could do with validating that this is a valid blob container name.
			_aggregateTypeShortName = aggregateName;
	}

	public T FulfilRequirements(T aggregate)
	{
		_aggregateRequirementsManager.Fulfil(aggregate);

		return aggregate;
	}

	async Task UpdateCacheAsync(T aggregate, DistributedCacheEntryOptions? cacheEntryOptions, CancellationToken cancellationToken = default)
	{
		cacheEntryOptions = GetCacheEntryOptions(cacheEntryOptions);

		try
		{
			var cacheKey = CreateCacheKey(aggregate.Id());
			if (aggregate.Details.Locked || (aggregate.Details.IsDeleted && _eventStoreOptions.Value.RemoveDeletedFromCache))
				await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
			else
			{
				if (!_eventStoreOptions.Value.CacheMode.HasFlag(EventStoreCachingOptions.StoreInCache))
					return;

				var data = SerializeSnapshot(aggregate);
				await _distributedCache.SetStringAsync(cacheKey, data, cacheEntryOptions, cancellationToken);
			}
		}
		catch (Exception ex)
		{
			_eventStoreTelemetry.CacheUpdateFailure(aggregate.Id(), _aggregateTypeFullName, ex);
		}
	}

	DistributedCacheEntryOptions GetCacheEntryOptions(DistributedCacheEntryOptions? cacheEntryOptions)
		=> cacheEntryOptions ?? new()
		{
			SlidingExpiration = _eventStoreOptions.Value.DefaultCacheSlidingDuration,
		};

	public async IAsyncEnumerable<string> GetAggregateIdsAsync(bool includeDeleted, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		List<string> tableColumns = [nameof(ITableEntity.PartitionKey)];
		if (!includeDeleted)
			tableColumns.Add(nameof(StreamVersionEntity.IsDeleted));

		var query = _tableClient.QueryEnumerableAsync<StreamVersionEntity>(m => m.RowKey == TableEventStoreConstants.StreamVersionRowKey, fields: tableColumns, cancellationToken: cancellationToken);
		await foreach (var entity in query)
		{
			if (includeDeleted || !entity.IsDeleted)
				yield return entity.PartitionKey;
		}
	}

	async Task<StreamVersionEntity?> GetStreamVersionAsync(string aggregateId, bool expectedToExist, CancellationToken cancellationToken)
	{
		_eventStoreTelemetry.GetStreamVersionStart(aggregateId, TableEventStoreConstants.StreamVersionRowKey);

		var elapsedMilliseconds = 0L;
		StreamVersionEntity? result = null;
		try
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

			result = await _tableClient.GetAsync<StreamVersionEntity>(aggregateId, TableEventStoreConstants.StreamVersionRowKey, cancellationToken);
			sw.Stop();

			elapsedMilliseconds = sw.ElapsedMilliseconds;

			if (result == null)
			{
				if (expectedToExist)
					_eventStoreTelemetry.StreamVersionExpectedToExistButNotFound(aggregateId);
				else
					_eventStoreTelemetry.StreamVersionNotFound(aggregateId);
			}
			else
				_eventStoreTelemetry.StreamVersionFound(aggregateId, result.Version, result.AggregateType, result.IsDeleted);
		}
		catch (Exception ex)
		{
			_eventStoreTelemetry.GetStreamVersionFailed(aggregateId, TableEventStoreConstants.StreamVersionRowKey, ex);
		}

		_eventStoreTelemetry.GetStreamVersionComplete(aggregateId, TableEventStoreConstants.StreamVersionRowKey, elapsedMilliseconds);

		return result;
	}

	static bool ReturnAggregate(bool isDeleted, string aggregateId, EventStoreOperationContext context)
	{
		if (isDeleted)
		{
			switch (context.DeleteMode)
			{
				case DeleteHandlingMode.ThrowsException:
					throw AggregateIsDeletedException(aggregateId);
				case DeleteHandlingMode.ReturnsNull:
					return false;
			}
		}

		return true;
	}

	string CreateEventRowKey(int version)
		=> $"{_eventStoreOptions.Value.EventPrefix}_{$"{version}".PadLeft(_eventStoreOptions.Value.EventSuffixLength, '0')}";

	static string CreateIdempotencyCheckRowKey(string idempotencyId)
		=> $"{TableEventStoreConstants.IdempotencyCheckRowKeyPrefix}{idempotencyId}";

	string GenerateEventBlobName(string aggregateId, string eventId)
		=> $"{_aggregateTypeShortName}/{aggregateId}/{eventId}.json".ToLowerSafe();

	public string GenerateSnapshotBlobName(string aggregateId)
		=> $"{GenerateSnapshotBlobPath(aggregateId)}/{TableEventStoreConstants.SnapshotFilename}".ToLowerSafe();

	public string GenerateSnapshotBlobPath(string aggregateId)
		=> $"{_aggregateTypeShortName}/{aggregateId}".ToLowerSafe();

	public string CreateCacheKey(string aggregateId)
		=> $"{_aggregateTypeShortName}:{aggregateId}".ToLowerSafe();
}
