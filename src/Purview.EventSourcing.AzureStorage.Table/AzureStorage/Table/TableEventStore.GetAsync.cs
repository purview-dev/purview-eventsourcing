﻿using Microsoft.Extensions.Caching.Distributed;
using Purview.EventSourcing.Aggregates.Events;
using Purview.EventSourcing.AzureStorage.Table.Entities;

namespace Purview.EventSourcing.AzureStorage.Table;

partial class TableEventStore<T>
{
	[System.Diagnostics.DebuggerStepThrough]
	public Task<T?> GetAsync(string id, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
		=> GetCoreAsync(id, operationContext, cancellationToken);

	async Task<T?> GetCoreAsync(string aggregateId, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId, nameof(aggregateId));

		operationContext ??= EventStoreOperationContext.DefaultContext;

		_eventStoreLog.GetAggregateStart(aggregateId, _aggregateTypeFullName);
		var getStopwatch = System.Diagnostics.Stopwatch.StartNew();
		try
		{
			var aggregate = operationContext.CacheMode.HasFlag(EventStoreCachingOptions.GetFromCache)
				? await GetFromCacheAsync(aggregateId, cancellationToken)
				: null;

			if (aggregate != null)
			{
				_eventStoreLog.AggregateRetrievedFromCache(aggregateId, _aggregateTypeFullName);

				return ReturnAggregate(aggregate.Details.IsDeleted, aggregateId, operationContext)
					? PrepareAggregateForReturn(aggregate, _aggregateRequirementsManager)
					: null;
			}

			var streamVersion = await GetStreamVersionAsync(aggregateId, true, cancellationToken);
			if (streamVersion == null)
				return null;

			if (!ReturnAggregate(streamVersion.IsDeleted, aggregateId, operationContext))
				return null;

			//** THIS ISN'T CORRECT, SO WE'RE PASSING IN NULL TO ENABLE ALL ADDITIONAL EVENTS **//
			// It's only odd as in I haven't thought it through completely...

			//int? streamVersionIdentifier = streamVersion.Version;
			//if (streamVersionIdentifier < 1)
			//	// Passing in null for the maxVersion, because we want it to go and find all additional events
			//	// because we don't have a valid version.
			//	streamVersionIdentifier = null;

			// Passing in null for the maxVersion, because we want it to go and find all additional events
			int? streamVersionIdentifier = null;
			if (!operationContext.SkipSnapshot)
			{
				// If there's no snapshot, then this isn't doing a create (as in brand new
				// never before seen object), we know it exists, it has a stream version...
				// This is for the case when there is no snapshot, and we need to create the entity and apply the events.
				aggregate = await GetLatestSnapshotAsync(aggregateId, cancellationToken);
			}

			aggregate ??= new T
			{
				Details = {
					Id = aggregateId
				}
			};

			await GetAndApplyEventsAsync(aggregate, streamVersion, streamVersionIdentifier, cancellationToken);
			await UpdateCacheAsync(aggregate, operationContext.CacheOptions, cancellationToken);

			return PrepareAggregateForReturn(aggregate, _aggregateRequirementsManager);
		}
		catch (Exception ex)
		{
			_eventStoreLog.GetAggregateFailed(aggregateId, _aggregateTypeFullName, ex);
			throw;
		}
		finally
		{
			getStopwatch.Stop();
			_eventStoreLog.GetAggregateComplete(aggregateId, _aggregateTypeFullName, getStopwatch.ElapsedMilliseconds);
		}

		static T PrepareAggregateForReturn(T aggregate, Services.IAggregateRequirementsManager aggregateRequirementsManager)
		{
			var currentVersion = aggregate.Details.CurrentVersion;

			aggregate.ClearUnsavedEvents();

			aggregate.Details.CurrentVersion = currentVersion;

			aggregateRequirementsManager.Fulfil(aggregate);

			return aggregate;
		}
	}

	async Task GetAndApplyEventsAsync(T aggregate, StreamVersionEntity streamVersion, int? maxVersion, CancellationToken cancellationToken)
	{
		var aggregateId = aggregate.Id();
		var eventCount = 0;
		var everQuery = GetEventRangeAsync(aggregateId, aggregate.Details.SnapshotVersion + 1, maxVersion, cancellationToken);
		await foreach (var eventResult in everQuery)
		{
			var @event = eventResult.@event;
			if (@event is UnknownEvent || !aggregate.CanApplyEvent(@event))
			{
				var eventType = @event.GetType();
				if (@event is UnknownEvent)
					_eventStoreLog.SkippedUnknownEvent(aggregateId, _aggregateTypeFullName, aggregate.AggregateType, eventResult.eventType, @event.Details.AggregateVersion);
				else
					_eventStoreLog.CannotApplyEvent(aggregateId, _aggregateTypeFullName, aggregate.AggregateType, eventResult.eventType, eventType.FullName ?? eventType.Name, @event.Details.AggregateVersion);

				// Without doing this, you won't be able to write to this aggregate anymore.
				aggregate.Details.CurrentVersion = @event.Details.AggregateVersion;
			}
			else
				aggregate.ApplyEvent(@event);

			eventCount++;
		}

		_eventStoreLog.ReconstitutedAggregateFromEvents(aggregateId, _aggregateTypeFullName, aggregate.AggregateType, eventCount, AggregateVersionData.Create(aggregate));

		aggregate.Details.SavedVersion = aggregate.Details.CurrentVersion;
		aggregate.Details.Etag = streamVersion.ETag.ToString();

		// Make sure we set is deleted too, in-case we're being allowed to get deleted aggregates.
		aggregate.Details.IsDeleted = streamVersion.IsDeleted;
	}

	async Task<T?> GetLatestSnapshotAsync(string aggregateId, CancellationToken cancellationToken)
	{
		var aggregateBlobName = GenerateSnapshotBlobName(aggregateId);
		if (!await _blobClient.ExistsAsync(aggregateBlobName, cancellationToken))
			return null;

		try
		{
			using var blobStream = await _blobClient.GetStreamAsync(aggregateBlobName, cancellationToken);
			if (blobStream == null)
				return null;

			using StreamReader reader = new(blobStream);
			var aggregateContent = await reader.ReadToEndAsync(cancellationToken);

			return DeserializeSnapshot(aggregateContent);
		}
		catch (Exception ex)
		{
			_eventStoreLog.SnapshotDeserializationFailed(aggregateId, _aggregateTypeFullName, ex);

			return null;
		}
	}

	async Task<T?> GetFromCacheAsync(string aggregateId, CancellationToken cancellationToken)
	{
		T? aggregate = null;
		try
		{
			var cacheKey = CreateCacheKey(aggregateId);
			var snapshotData = await _cache.GetStringAsync(cacheKey, cancellationToken);
			if (!string.IsNullOrWhiteSpace(snapshotData))
			{
				aggregate = DeserializeSnapshot(snapshotData);
				aggregate.Details.SavedVersion = aggregate.Details.CurrentVersion;
			}
		}
		catch (Exception ex)
		{
			_eventStoreLog.CacheGetFailure(aggregateId, _aggregateTypeFullName, ex);
		}

		return aggregate;
	}
}
