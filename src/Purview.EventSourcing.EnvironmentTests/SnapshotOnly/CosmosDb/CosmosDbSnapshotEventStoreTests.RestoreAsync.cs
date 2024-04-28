using Microsoft.Azure.Cosmos;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.CosmosDb;

namespace Purview.EventSourcing.CosmosDb.Snapshot;

partial class CosmosDbSnapshotEventStoreTests
{
	[Fact]
	public async Task RestoreAsync_GivenExistingAggregateMarkedAsDeletedAndDoesNotExistInCosmosDbWhenRestore_SnapshotCreatedInCosmosDb()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext();

		string aggregateId = Guid.NewGuid().ToString();
		PersistenceAggregate aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		PartitionKey partitionKey = new(aggregate.AggregateType);

		bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
		saveResult
			.Should()
			.BeTrue();

		PersistenceAggregate? aggregateFromCosmosDb = await context.CosmosDbClient.GetAsync<PersistenceAggregate>(aggregateId, partitionKey, cancellationToken: tokenSource.Token);
		aggregateFromCosmosDb
			.Should()
			.NotBeNull();

		bool deleteResult = await context.EventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);
		deleteResult
			.Should()
			.BeTrue();

		aggregateFromCosmosDb = await context.CosmosDbClient.GetAsync<PersistenceAggregate>(aggregateId, partitionKey, cancellationToken: tokenSource.Token);
		aggregateFromCosmosDb
			.Should()
			.BeNull();

		// Act
		bool restoreResult = await context.EventStore.RestoreAsync(aggregate, cancellationToken: tokenSource.Token);

		aggregateFromCosmosDb = await context.CosmosDbClient.GetAsync<PersistenceAggregate>(aggregateId, partitionKey, cancellationToken: tokenSource.Token);

		// Assert
		restoreResult
			.Should()
			.BeTrue();

		aggregateFromCosmosDb
			.Should()
			.NotBeNull();
	}
}
