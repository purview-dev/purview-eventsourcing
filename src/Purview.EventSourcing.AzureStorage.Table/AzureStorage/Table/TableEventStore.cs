using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Azure.Data.Tables;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.AzureStorage.Table.Entities;
using Purview.EventSourcing.AzureStorage.Table.Options;
using Purview.EventSourcing.Services;

namespace Purview.EventSourcing.AzureStorage.Table;

public sealed partial class TableEventStore<T> : ITableEventStore<T>
	where T : class, IAggregate, new()
{
	const int _serializationBufferSize = 4096;
	const int _maxEventSize = 32000;

	readonly StorageClients.Table.AzureTableClient _tableClient;
	readonly StorageClients.Blob.AzureBlobClient _blobClient;

	readonly IAggregateEventNameMapper _eventNameMapper;
	readonly IOptions<AzureStorageEventStoreOptions> _eventStoreOptions;
	readonly FluentValidation.IValidator<T>? _validator;
	readonly IAggregateIdFactory? _aggregateIdFactory;
	readonly IDistributedCache _cache;
	readonly ITableEventStoreTelemetry _eventStoreLog;
	readonly ChangeFeed.IAggregateChangeFeedNotifier<T> _aggregateChangeNotifier;
	readonly IAggregateRequirementsManager _aggregateRequirementsManager;

	readonly string _aggregateTypeFullName;
	readonly string _aggregateTypeShortName;

	public TableEventStore(
		IAggregateEventNameMapper eventNameMapper,
		[NotNull] IOptions<AzureStorageEventStoreOptions> azureStorageOptions,
		IDistributedCache cache,
		ITableEventStoreTelemetry eventStoreLog,
		ChangeFeed.IAggregateChangeFeedNotifier<T> aggregateChangeNotifier,
		IAggregateRequirementsManager aggregateRequirementsManager,
		FluentValidation.IValidator<T>? validator = null,
		ITableEventStoreStorageNameBuilder? nameBuilder = null,
		IAggregateIdFactory? aggregateIdFactory = null)
	{
		_eventNameMapper = eventNameMapper;
		_eventStoreOptions = azureStorageOptions;
		_validator = validator;
		_aggregateIdFactory = aggregateIdFactory;
		_cache = cache;
		_eventStoreLog = eventStoreLog;
		_aggregateChangeNotifier = aggregateChangeNotifier;
		_aggregateRequirementsManager = aggregateRequirementsManager;

		var name = typeof(T).Name;
		var tableName = nameBuilder?.GetTableName<T>() ?? $"{azureStorageOptions.Value.Table}{name}";
		var containerName = nameBuilder?.GetBlobContainerName<T>() ?? azureStorageOptions.Value.Container;

		_tableClient = new(azureStorageOptions.Value, tableName);
		_blobClient = new(azureStorageOptions.Value, containerName);

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
				await _cache.RemoveAsync(cacheKey, cancellationToken);
			else
			{
				if (!_eventStoreOptions.Value.CacheMode.HasFlag(EventStoreCachingOptions.StoreInCache))
					return;

				var data = SerializeSnapshot(aggregate);
				await _cache.SetStringAsync(cacheKey, data, cacheEntryOptions, cancellationToken);
			}
		}
		catch (Exception ex)
		{
			_eventStoreLog.CacheUpdateFailure(aggregate.Id(), _aggregateTypeFullName, ex);
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
		_eventStoreLog.GetStreamVersionStart(aggregateId, TableEventStoreConstants.StreamVersionRowKey);

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
					_eventStoreLog.StreamVersionExpectedToExistButNotFound(aggregateId);
				else
					_eventStoreLog.StreamVersionNotFound(aggregateId);
			}
			else
				_eventStoreLog.StreamVersionFound(aggregateId, result.Version, result.AggregateType, result.IsDeleted);
		}
		catch (Exception ex)
		{
			_eventStoreLog.GetStreamVersionFailed(aggregateId, TableEventStoreConstants.StreamVersionRowKey, ex);
		}

		_eventStoreLog.GetStreamVersionComplete(aggregateId, TableEventStoreConstants.StreamVersionRowKey, elapsedMilliseconds);

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
