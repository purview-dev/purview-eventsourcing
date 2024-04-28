using System.Text;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs.Models;
using Purview.Interfaces.Storage.AzureStorage.Table;

namespace Purview.EventSourcing.AzureStorage.Table;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task DeleteAsync_GivenAggregateExists_PermanentlyDeletesAllData()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate? aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: 2);

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		aggregate = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);
		if (aggregate == null)
		{
			throw new NullReferenceException();
		}

		// Act
		bool result = await eventStore.DeleteAsync(aggregate, new EventStoreOperationContext
		{
			PermanentlyDelete = true
		}, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();

		aggregate
			.Details
			.IsDeleted
			.Should()
			.BeTrue();

		aggregate
			.Details
			.Locked
			.Should()
			.BeTrue();

		await ValidateEntitiesDeletedAsync(aggregate, eventStore, tokenSource.Token);
	}

	public async Task DeleteAsync_GivenAggregateExistsWithLargeEvent_PermanentlyDeletesAllData()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate? aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		string value = string.Empty;
		bool sizeIsLessThan32K = true;
		while (sizeIsLessThan32K)
		{
			value += "abcdefghijklmnopqrstvwxyz";
			value += "ABCDEFGHIJKLMNOPQRSTVWXYZ";
			value += "1234567890";

			sizeIsLessThan32K = Encoding.UTF8.GetByteCount(value) < short.MaxValue;
		}

		aggregate.AppendString(value);

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: 2);

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		aggregate = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);

		if (aggregate == null)
		{
			throw new NullReferenceException();
		}

		// Act
		bool result = await eventStore.DeleteAsync(aggregate, new EventStoreOperationContext
		{
			PermanentlyDelete = true
		}, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();

		aggregate
			.Details
			.IsDeleted
			.Should()
			.BeTrue();

		aggregate
			.Details
			.Locked
			.Should()
			.BeTrue();

		await ValidateEntitiesDeletedAsync(aggregate, eventStore, tokenSource.Token);
	}

	async Task ValidateEntitiesDeletedAsync(TAggregate aggregate, TableEventStore<TAggregate> eventStore, CancellationToken cancellationToken)
	{
		TableQueryResponse<TableEntity> results = await fixture.TableClient.QueryAsync<TableEntity>(m => m.PartitionKey == aggregate.Details.Id, cancellationToken: cancellationToken);

		results
			.Results
			.Should()
			.BeEmpty();

		string prefix = eventStore.GenerateSnapshotBlobPath(aggregate.Id());

		AsyncPageable<BlobItem> blobResults = await fixture.BlobClient.GetBlobsAsync(prefix, cancellationToken: cancellationToken);
		IEnumerable<BlobItem> blobsToDelete = blobResults.ToBlockingEnumerable(cancellationToken: cancellationToken);

		blobsToDelete
			.Should()
			.BeEmpty();
	}
}
