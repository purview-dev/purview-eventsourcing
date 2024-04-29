using MongoDB.Driver;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.MongoDb;

namespace Purview.EventSourcing.MongoDb.Snapshot;

partial class MongoDbSnapshotEventStoreTests
{
	[Fact]
	public async Task RestoreAsync_GivenExistingAggregateMarkedAsDeletedAndDoesNotExistInMongoDbWhenRestore_SnapshotCreatedInMongoDb()
	{
		// Arrange
		using CancellationTokenSource tokenSource = TestHelpers.CancellationTokenSource();
		using MongoDbSnapshotTestContext context = fixture.CreateContext();

		string aggregateId = Guid.NewGuid().ToString();
		PersistenceAggregate aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		MongoDbSnapshotEventStore<PersistenceAggregate> mongoDbEventStore = context.EventStore;

		bool saveResult = await mongoDbEventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
		saveResult
			.Should()
			.BeTrue();

		FilterDefinition<PersistenceAggregate> predicate = PredicateId(aggregateId);

		PersistenceAggregate? aggregateFromMongo = await context.MongoDbClient.GetAsync<PersistenceAggregate>(predicate, cancellationToken: tokenSource.Token);
		aggregateFromMongo
			.Should()
			.NotBeNull();

		bool deleteResult = await mongoDbEventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);
		deleteResult
			.Should()
			.BeTrue();

		aggregateFromMongo = await context.MongoDbClient.GetAsync<PersistenceAggregate>(aggregateId, cancellationToken: tokenSource.Token);
		aggregateFromMongo
			.Should()
			.BeNull();

		// Act
		bool restoreResult = await mongoDbEventStore.RestoreAsync(aggregate, cancellationToken: tokenSource.Token);

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
