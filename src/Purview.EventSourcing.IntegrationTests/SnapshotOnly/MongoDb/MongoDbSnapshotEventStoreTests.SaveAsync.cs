namespace Purview.EventSourcing.MongoDb.Snapshot;

partial class MongoDbSnapshotEventStoreTests
{
	[Fact]
	public async Task SaveAsync_GivenNewAggregateWithChanges_SavesAggregate()
	{
		// Arrange
		using var tokenSource = TestHelpers.CancellationTokenSource();
		var context = fixture.CreateContext();

		var aggregateId = Guid.NewGuid().ToString();
		var aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();
		aggregate.AppendString(aggregateId);

		var mongoDbEventStore = context.EventStore;

		// Act
		bool result = await mongoDbEventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();

		aggregate
			.IsNew()
			.Should()
			.BeFalse();

		var builder = PredicateId(aggregateId);

		// Verify by re-getting the aggregate directly from the MongoClient, not via the event store.
		var aggregateFromMongoDb = await context.MongoDbClient.GetAsync(builder, cancellationToken: tokenSource.Token);

		aggregateFromMongoDb
			.Should()
			.NotBeNull();

		aggregateFromMongoDb!
			.Id()
			.Should()
			.Be(aggregate.Id());

		aggregateFromMongoDb!
			.IncrementInt32
			.Should()
			.Be(aggregate.IncrementInt32);

		aggregateFromMongoDb
			.StringProperty
			.Should()
			.Be(aggregateId);

		aggregateFromMongoDb
			.Details
			.SavedVersion
			.Should()
			.Be(aggregate.Details.SavedVersion);

		aggregateFromMongoDb
			.Details
			.CurrentVersion
			.Should()
			.Be(aggregate.Details.CurrentVersion);

		aggregateFromMongoDb
			.Details
			.SnapshotVersion
			.Should()
			.Be(aggregate.Details.SnapshotVersion);

		aggregateFromMongoDb
			.Details
			.Etag
			.Should()
			.Be(aggregate.Details.Etag);
	}
}
