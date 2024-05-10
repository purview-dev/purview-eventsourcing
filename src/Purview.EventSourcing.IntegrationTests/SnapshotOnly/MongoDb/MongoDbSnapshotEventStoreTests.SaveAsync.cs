namespace Purview.EventSourcing.MongoDB.Snapshot;

partial class MongoDBSnapshotEventStoreTests
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
		var aggregateFromMongoDB = await context.MongoDBClient.GetAsync(builder, cancellationToken: tokenSource.Token);

		aggregateFromMongoDB
			.Should()
			.NotBeNull();

		aggregateFromMongoDB!
			.Id()
			.Should()
			.Be(aggregate.Id());

		aggregateFromMongoDB!
			.IncrementInt32
			.Should()
			.Be(aggregate.IncrementInt32);

		aggregateFromMongoDB
			.StringProperty
			.Should()
			.Be(aggregateId);

		aggregateFromMongoDB
			.Details
			.SavedVersion
			.Should()
			.Be(aggregate.Details.SavedVersion);

		aggregateFromMongoDB
			.Details
			.CurrentVersion
			.Should()
			.Be(aggregate.Details.CurrentVersion);

		aggregateFromMongoDB
			.Details
			.SnapshotVersion
			.Should()
			.Be(aggregate.Details.SnapshotVersion);

		aggregateFromMongoDB
			.Details
			.Etag
			.Should()
			.Be(aggregate.Details.Etag);
	}
}
