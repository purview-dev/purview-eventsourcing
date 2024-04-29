using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.CosmosDb;

namespace Purview.EventSourcing.CosmosDb.Snapshot;

partial class CosmosDbSnapshotEventStoreTests
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
	public async Task CanQuery_GivenAggregatesExist_QueryAsExpected(int numberOfAggregates, int numberOfEvents)
	{
		// Arrange
		using CancellationTokenSource tokenSource = TestHelpers.CancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext(correlationIdsToGenerate: numberOfAggregates);

		for (int aggregateIndex = 0; aggregateIndex < numberOfAggregates; aggregateIndex++)
		{
			PersistenceAggregate aggregate = CreateAggregate($"{aggregateIndex}_{context.RunId}");

			for (int eventIndex = 0; eventIndex < numberOfEvents; eventIndex++)
			{
				aggregate.IncrementInt32Value();
			}

			bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult.Should().BeTrue();
		}

		// Act
		IEnumerable<PersistenceAggregate> aggregates = (await context.EventStore.QueryAsync(m => m.IncrementInt32 == numberOfEvents, maxRecordCount: numberOfAggregates + 1, cancellationToken: tokenSource.Token)).Results;

		// Assert
		aggregates.Should().HaveCount(numberOfAggregates);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(10)]
	public async Task CanQuery_GivenAggregateType_QueryAsExpected(int numberOfAggregates)
	{
		// Arrange
		using CancellationTokenSource tokenSource = TestHelpers.CancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext(correlationIdsToGenerate: numberOfAggregates);

		string aggregateType = CreateAggregate().AggregateType;

		for (int aggregateIndex = 0; aggregateIndex < numberOfAggregates; aggregateIndex++)
		{
			PersistenceAggregate aggregate = CreateAggregate($"{aggregateIndex}_{context.RunId}");
			aggregate.IncrementInt32Value();

			bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult.Should().BeTrue();
		}

		// Act
		IEnumerable<PersistenceAggregate> aggregates = (await context.EventStore.QueryAsync(m => m.AggregateType == aggregateType, maxRecordCount: numberOfAggregates + 1, cancellationToken: tokenSource.Token)).Results;

		// Assert
		aggregates.Should().HaveCount(numberOfAggregates);
	}
}
