using Purview.EventSourcing.Aggregates.Persistence;

namespace Purview.EventSourcing.MongoDb.Snapshot;

partial class MongoDbSnapshotEventStoreTests
{
	[Fact]
	public async Task RestoreAsync_GivenExistingAggregateMarkedAsDeletedAndDoesNotExistInMongoDbWhenRestore_SnapshotCreatedInMongoDb()
	{
		// Arrange
		using var tokenSource = TestHelpers.CancellationTokenSource();
		var context = fixture.CreateContext();

		var aggregateId = Guid.NewGuid().ToString();
		var aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		var mongoDbEventStore = context.EventStore;

		bool saveResult = await mongoDbEventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
		saveResult
			.Should()
			.BeTrue();

		var predicate = PredicateId(aggregateId);

		var aggregateFromMongo = await context.MongoDbClient.GetAsync(predicate, cancellationToken: tokenSource.Token);
		aggregateFromMongo
			.Should()
			.NotBeNull();

		var deleteResult = await mongoDbEventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);
		deleteResult
			.Should()
			.BeTrue();

		aggregateFromMongo = await context.MongoDbClient.GetAsync<PersistenceAggregate>(aggregateId, cancellationToken: tokenSource.Token);
		aggregateFromMongo
			.Should()
			.BeNull();

		// Act
		var restoreResult = await mongoDbEventStore.RestoreAsync(aggregate, cancellationToken: tokenSource.Token);

		aggregateFromMongo = await context.MongoDbClient.GetAsync<PersistenceAggregate>(aggregateId, cancellationToken: tokenSource.Token);

		// Assert
		restoreResult
			.Should()
			.BeTrue();

		aggregateFromMongo
			.Should()
			.NotBeNull();
	}
}
