using Microsoft.Azure.Cosmos;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.CosmosDb;

namespace Purview.EventSourcing.CosmosDb.Snapshot;

partial class CosmosDbSnapshotEventStoreTests
{
	[Fact]
	public async Task DeleteAsync_GivenExistingAggregateMarkedAsDeleted_DeletesFromCosmosDb()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext();

		string aggregateId = Guid.NewGuid().ToString();
		PersistenceAggregate aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		PartitionKey partitionKey = new(aggregate.AggregateType);

		bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
		saveResult.Should().BeTrue();

		PersistenceAggregate? aggregateFromCosmosDb = await context.CosmosDbClient.GetAsync<PersistenceAggregate>(aggregateId, partitionKey, cancellationToken: tokenSource.Token);
		aggregateFromCosmosDb.Should().NotBeNull();

		// Act
		bool deleteResult = await context.EventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);

		aggregateFromCosmosDb = await context.CosmosDbClient.GetAsync<PersistenceAggregate>(aggregateId, partitionKey, cancellationToken: tokenSource.Token);

		// Assert
		deleteResult.Should().BeTrue();

		aggregateFromCosmosDb.Should().BeNull();
	}
}
