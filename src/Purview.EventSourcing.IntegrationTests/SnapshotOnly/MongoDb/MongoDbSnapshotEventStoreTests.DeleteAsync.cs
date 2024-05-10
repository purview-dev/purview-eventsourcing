﻿using Purview.EventSourcing.Aggregates.Persistence;

namespace Purview.EventSourcing.MongoDB.Snapshot;

partial class MongoDBSnapshotEventStoreTests
{
	[Fact]
	public async Task DeleteAsync_GivenExistingAggregateMarkedAsDeleted_DeletesFromMongoDB()
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
		var aggregateFromMongoDB = await context.MongoDBClient.GetAsync(predicate, cancellationToken: tokenSource.Token);
		aggregateFromMongoDB
			.Should()
			.NotBeNull();

		// Act
		var deleteResult = await mongoDbEventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);

		aggregateFromMongoDB = await context.MongoDBClient.GetAsync<PersistenceAggregate>(a => a.Details.Id == aggregateId, cancellationToken: tokenSource.Token);

		// Assert
		deleteResult
			.Should()
			.BeTrue();

		aggregateFromMongoDB
			.Should()
			.BeNull();
	}
}
