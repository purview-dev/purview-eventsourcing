using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using LinqKit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.MongoDb.Entities;
using Purview.EventSourcing.Services;

namespace Purview.EventSourcing.MongoDb;

public sealed partial class MongoDbEventStore<T> : IMongoDbEventStore<T>
	where T : class, IAggregate, new()
{
	readonly StorageClients.MongoDbClient _client;

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
		[NotNull] IOptions<MongoDbEventStoreOptions> mongoDbOptions,
		IDistributedCache distributedCache,
		IMongoDbEventStoreTelemetry eventStoreTelemetry,
		ChangeFeed.IAggregateChangeFeedNotifier<T> aggregateChangeNotifier,
		IAggregateRequirementsManager aggregateRequirementsManager,
		FluentValidation.IValidator<T>? validator = null,
		IAggregateIdFactory? aggregateIdFactory = null)
	{
		_eventNameMapper = eventNameMapper;
		_eventStoreOptions = mongoDbOptions;
		_validator = validator;
		_aggregateIdFactory = aggregateIdFactory;
		_distributedCache = distributedCache;
		_eventStoreTelemetry = eventStoreTelemetry;
		_aggregateChangeNotifier = aggregateChangeNotifier;
		_aggregateRequirementsManager = aggregateRequirementsManager;


		_aggregateTypeShortName = typeof(T).Name;
		_aggregateTypeFullName = typeof(T).FullName ?? _aggregateTypeShortName;

		var aggregateName = _eventNameMapper.InitializeAggregate<T>();
		if (!aggregateName.Contains('.', StringComparison.InvariantCulture))
			// Could do with validating that this is a valid blob container name.
			_aggregateTypeShortName = aggregateName;

		_client = new(new StorageClients.MongoDbConfiguration
		{
			ApplicationName = mongoDbOptions.Value.ApplicationName,
			Database = mongoDbOptions.Value.Database,
			Collection = mongoDbOptions.Value.Collection ?? $"es-{_aggregateTypeShortName}-events",
			ConnectionString = mongoDbOptions.Value.ConnectionString
		});
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
		var whereClause = PredicateBuilder.New<StreamVersionEntity>(m => m.AggregateType == _aggregateTypeShortName && m.EntityType == EntityTypes.StreamVersionType);
		if (!includeDeleted)
			whereClause = PredicateBuilder.And<StreamVersionEntity>(whereClause, m => !m.IsDeleted);

		var query = _client.QueryEnumerableAsync<StreamVersionEntity>(whereClause, cancellationToken: cancellationToken);
		await foreach (var entity in query)
		{
			if (includeDeleted || !entity.IsDeleted)
				yield return entity.Id;
		}
	}

	async Task<StreamVersionEntity?> GetStreamVersionAsync(string aggregateId, bool expectedToExist, CancellationToken cancellationToken)
	{
		_eventStoreTelemetry.GetStreamVersionStart(aggregateId);

		var elapsedMilliseconds = 0L;
		StreamVersionEntity? result = null;
		try
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

			result = await _client.GetAsync<StreamVersionEntity>(CreateStreamVersionId(aggregateId), cancellationToken);
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
			_eventStoreTelemetry.GetStreamVersionFailed(aggregateId, ex);
		}

		_eventStoreTelemetry.GetStreamVersionComplete(aggregateId, elapsedMilliseconds);

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
	string CreateStreamVersionId(string aggregateId)
		=> $"s_{_aggregateTypeShortName}_{aggregateId}";

	string CreateEventId(string aggregateId, int version)
		=> $"e_{_aggregateTypeShortName}_{aggregateId}_{$"{version}".PadLeft(_eventStoreOptions.Value.EventSuffixLength, '0')}";

	string CreateIdempotencyCheckId(string aggregateId, string idempotencyId)
		=> $"i_{_aggregateTypeShortName}_{aggregateId}_{idempotencyId}";

	public string CreateCacheKey(string aggregateId)
		=> $"{_aggregateTypeShortName}:{aggregateId}".ToLowerSafe();
}
