﻿using Azure.Data.Tables;
using Azure.Storage.Blobs.Models;
using Purview.EventSourcing.Aggregates.Events;
using Purview.EventSourcing.AzureStorage.Table.StorageClients.Table;

namespace Purview.EventSourcing.AzureStorage.Table;

partial class TableEventStore<T>
{
	public async Task<bool> DeleteAsync(T aggregate, EventStoreOperationContext? operationContext, CancellationToken cancellationToken = default)
	{
		if (aggregate == null)
			throw NullAggregate(aggregate);

		if (aggregate.Details.IsDeleted)
			throw AggregateIsDeletedException(aggregate.Id());

		operationContext ??= EventStoreOperationContext.DefaultContext;

		if (aggregate.IsNew())
			return false;

		if (operationContext.PermanentlyDelete)
			return await PermanentlyDeleteAsync(aggregate, cancellationToken);

		DeleteEvent deleteAggregateEvent = new()
		{
			Details = {
				AggregateVersion = aggregate.Details.CurrentVersion + 1,
				When = DateTimeOffset.UtcNow
			}
		};
		aggregate.ApplyEvent(deleteAggregateEvent);

		var result = await SaveCoreAsync(aggregate, operationContext, cancellationToken, deleteAggregateEvent);

		return result.Saved;
	}

	async Task<bool> PermanentlyDeleteAsync(T aggregate, CancellationToken cancellationToken = default)
	{
		if (aggregate == null)
			throw NullAggregate(aggregate);

		var aggregateId = aggregate.Id();
		var streamVersion = await GetStreamVersionAsync(aggregateId, true, cancellationToken);
		if (streamVersion == null)
			return false;

		_eventStoreTelemetry.PermanentDeleteRequested(aggregateId);

		const int entitiesInEachBatch = 20;
		try
		{
			List<TableEntity> entitiesToDelete = [];
			var results = _tableClient.QueryEnumerableAsync<TableEntity>(m => m.PartitionKey == aggregate.Details.Id, fields: [nameof(TableEntity.PartitionKey), nameof(TableEntity.RowKey)], cancellationToken: cancellationToken);
			await foreach (var entity in results)
				entitiesToDelete.Add(entity);

			var batches = entitiesToDelete.Chunk(entitiesInEachBatch).Select(m =>
			{
				BatchOperation batch = new();
				foreach (var entity in m)
					batch.Delete(entity);

				return batch;
			});

			foreach (var batch in batches)
				await _tableClient.SubmitBatchAsync(batch, cancellationToken);

			var prefix = GenerateSnapshotBlobPath(aggregateId);

			var blobs = await _blobClient.GetBlobsAsync(prefix, cancellationToken);
			List<BlobItem> blobsToDelete = [];
			await foreach (var blob in blobs)
				blobsToDelete.Add(blob);

			foreach (var blob in blobsToDelete)
				await _blobClient.DeleteBlobIfExistsAsync(blob.Name, cancellationToken);

			_eventStoreTelemetry.PermanentDeleteComplete(aggregateId);

			aggregate.Details.IsDeleted = true;
			aggregate.Details.Locked = true;

			return true;
		}
		catch (Exception ex)
		{
			_eventStoreTelemetry.PermanentDeleteFailed(aggregateId, ex);

			return false;
		}
		finally
		{
			ClearCacheFireAndForget(aggregate);
		}
	}

	//		async Task<TableEntity?> GetIdempotencyMarkerAsync(string aggregateId, string idempotencyId, CancellationToken cancellationToken = default)
	//		{
	//			try
	//			{
	//				var tableClient = _tableClient.Value;
	//				var result = await tableClient.GetAsync<TableEntity>(aggregateId, CreateIdempotencyCheckRowKey(idempotencyId), cancellationToken);

	//				return result;
	//			}
	//#pragma warning disable CA1031 // Do not catch general exception types
	//			catch (Exception ex)
	//			{
	//				_eventStoreLog.GetIdempotencyMarkerFailed(aggregateId, idempotencyId, ex);
	//			}
	//#pragma warning restore CA1031 // Do not catch general exception types

	//			return null;
	//		}
}
