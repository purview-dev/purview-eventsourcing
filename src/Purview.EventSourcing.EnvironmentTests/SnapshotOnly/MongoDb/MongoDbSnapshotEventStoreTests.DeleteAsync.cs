using MongoDB.Driver;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.MongoDb;

namespace Purview.EventSourcing.MongoDb.Snapshot;

partial class MongoDbSnapshotEventStoreTests
{
	[Fact]
	public async Task DeleteAsync_GivenExistingAggregateMarkedAsDeleted_DeletesFromMongoDb()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
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

		PersistenceAggregate? aggregateFromMongoDb = await context.MongoDbClient.GetAsync<PersistenceAggregate>(predicate, cancellationToken: tokenSource.Token);
		aggregateFromMongoDb
			.Should()
			.NotBeNull();

		// Act
		bool deleteResult = await mongoDbEventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);

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
