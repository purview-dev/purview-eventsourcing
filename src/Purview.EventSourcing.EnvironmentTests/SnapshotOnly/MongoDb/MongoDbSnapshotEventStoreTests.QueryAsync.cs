using MongoDB.Driver;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.MongoDb;

namespace Purview.EventSourcing.MongoDb.Snapshot;

partial class MongoDbSnapshotEventStoreTests
{
	[Theory]
	[InlineData(1, 1)]
	[InlineData(1, 5)]
	[InlineData(1, 10)]
	[InlineData(5, 1)]
	[InlineData(5, 5)]
	[InlineData(5, 10)]
	[InlineData(10, 1)]
	[InlineData(10, 5)]
	[InlineData(10, 10)]
	public async Task QueryAsync_GivenAggregatesExist_QueriesAsExpected(int numberOfAggregates, int numberOfEvents)
	{
		// Arrange
		using CancellationTokenSource tokenSource = TestHelpers.CancellationTokenSource();
		using MongoDbSnapshotTestContext context = fixture.CreateContext(correlationIdsToGenerate: numberOfAggregates);

		MongoDbSnapshotEventStore<PersistenceAggregate> eventStore = context.EventStore;

		for (int aggregateIndex = 0; aggregateIndex < numberOfAggregates; aggregateIndex++)
		{
			PersistenceAggregate aggregate = CreateAggregate($"{aggregateIndex}_{context.RunId}");

			for (int eventIndex = 0; eventIndex < numberOfEvents; eventIndex++)
			{
				aggregate.IncrementInt32Value();
			}

			bool saveResult = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult
				.Should()
				.BeTrue();
		}

		// Act
		IEnumerable<PersistenceAggregate> aggregates = (await eventStore.QueryAsync(m => m.IncrementInt32 == numberOfEvents, cancellationToken: tokenSource.Token)).Results;

		// Assert
		aggregates
			.Count()
			.Should()
			.Be(numberOfAggregates);
	}
}
