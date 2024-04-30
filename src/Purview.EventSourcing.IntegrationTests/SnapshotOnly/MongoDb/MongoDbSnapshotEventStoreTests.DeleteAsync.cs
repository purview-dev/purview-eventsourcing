using Purview.EventSourcing.Aggregates.Persistence;

namespace Purview.EventSourcing.MongoDb.Snapshot;

partial class MongoDbSnapshotEventStoreTests
{
	[Fact]
	public async Task DeleteAsync_GivenExistingAggregateMarkedAsDeleted_DeletesFromMongoDb()
	{
		// Arrange
		using var tokenSource = TestHelpers.CancellationTokenSource();
		var context = fixture.CreateContext();

		var aggregateId = Guid.NewGuid().ToString();
		var aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		var mongoDbEventStore = context.EventStore;

		bool saveResult = await mongoDbEventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
		saveResult.Should().BeTrue();

		var predicate = PredicateId(aggregateId);
		var aggregateFromMongoDb = await context.MongoDbClient.GetAsync(predicate, cancellationToken: tokenSource.Token);
		aggregateFromMongoDb
			.Should()
			.NotBeNull();

		// Act
		var deleteResult = await mongoDbEventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);

		aggregateFromMongoDb = await context.MongoDbClient.GetAsync<PersistenceAggregate>(a => a.Details.Id == aggregateId, cancellationToken: tokenSource.Token);

		// Assert
		deleteResult
			.Should()
			.BeTrue();

		aggregateFromMongoDb
			.Should()
			.BeNull();
	}
}
