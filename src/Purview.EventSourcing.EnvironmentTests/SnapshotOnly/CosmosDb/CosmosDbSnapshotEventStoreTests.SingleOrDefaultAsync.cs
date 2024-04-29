using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.CosmosDb;

namespace Purview.EventSourcing.CosmosDb.Snapshot;

partial class CosmosDbSnapshotEventStoreTests
{
	[Fact]
	public async Task SingleOrDefaultAsync_GivenMultipleMatchingAggregates_ThrowsException()
	{
		const int matchingIncrement = 10;

		// Arrange
		using CancellationTokenSource tokenSource = TestHelpers.CancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext();

		for (int i = 0; i < matchingIncrement; i++)
		{
			PersistenceAggregate aggregate = CreateAggregate();
			for (int x = 0; x < matchingIncrement; x++)
			{
				aggregate.IncrementInt32Value();
			}

			SaveResult<PersistenceAggregate> saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult.ToBoolean().Should().BeTrue();
			saveResult.Skipped.Should().BeFalse();
		}

		// Act
		Func<Task> func = async () => await context.EventStore.SingleOrDefaultAsync(m => m.IncrementInt32 == matchingIncrement, cancellationToken: tokenSource.Token);

		// Assert
		await func
			.Should()
			.ThrowAsync<InvalidOperationException>()
			.WithMessage("Sequence contains more than one element");
	}

	[Fact]
	public async Task SingleOrDefaultAsync_GivenSingleMatchingAggregates_ReturnsAggregate()
	{
		const int matchingIncrement = 10;

		// Arrange
		using CancellationTokenSource tokenSource = TestHelpers.CancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext();

		string aggregateId = Guid.NewGuid().ToString();
		PersistenceAggregate aggregate = CreateAggregate(id: aggregateId);
		for (int x = 0; x < matchingIncrement; x++)
		{
			aggregate.IncrementInt32Value();
		}

		bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
		saveResult.Should().BeTrue();

		// Act
		PersistenceAggregate? result = await context.EventStore.SingleOrDefaultAsync(m => m.IncrementInt32 == matchingIncrement, cancellationToken: tokenSource.Token);

		// Assert
		result?.Id().Should().Be(aggregateId);
	}

	[Fact]
	public async Task SingleOrDefaultAsync_GivenNoMatchingAggregates_ReturnsNull()
	{
		const int aggregatesToCreate = 10;
		const int eventsToCreate = 10;

		// Arrange
		using CancellationTokenSource tokenSource = TestHelpers.CancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext();

		for (int i = 0; i < aggregatesToCreate; i++)
		{
			PersistenceAggregate aggregate = CreateAggregate();
			for (int x = 0; x < eventsToCreate; x++)
			{
				aggregate.IncrementInt32Value();
			}

			bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
			saveResult.Should().BeTrue();
		}

		// Act
		PersistenceAggregate? result = await context.EventStore.SingleOrDefaultAsync(m => m.IncrementInt32 == -1, cancellationToken: tokenSource.Token);

		// Assert
		result.Should().BeNull();
	}
}
