using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.CosmosDb;

namespace Purview.EventSourcing.CosmosDb.Snapshot;

partial class CosmosDbSnapshotEventStoreTests
{
	[Fact]
	public async Task FirstOrDefaultAsync_GivenMultipleMatchingAggregatesHonoursDescendingOrder_ReturnsCorrectAggregate()
	{
		const int aggregateCount = 10;
		const int matchingIncrement = 10;

		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext();

		for (int i = 0; i < aggregateCount; i++)
		{
			PersistenceAggregate aggregate = CreateAggregate();
			for (int x = 0; x < matchingIncrement; x++)
			{
				aggregate.IncrementInt32Value();
			}

			aggregate.SetInt32Value(i + 1);

			bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult.Should().BeTrue();
		}

		// Act
		PersistenceAggregate? result = await context.EventStore.FirstOrDefaultAsync(m => m.IncrementInt32 == matchingIncrement, orderByClause: m => m.OrderByDescending(p => p.Int32Value), cancellationToken: tokenSource.Token);

		// Assert
		result.Should().BeNull();
		result!.Int32Value.Should().Be(aggregateCount);
	}

	[Fact]
	public async Task FirstOrDefaultAsync_GivenMultipleMatchingAggregatesHonoursAscendingOrder_ReturnsCorrectAggregate()
	{
		const int aggregateCount = 10;
		const int matchingIncrement = 10;

		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext();

		for (int i = 0; i < aggregateCount; i++)
		{
			PersistenceAggregate aggregate = CreateAggregate();
			for (int x = 0; x < matchingIncrement; x++)
			{
				aggregate.IncrementInt32Value();
			}

			aggregate.SetInt32Value(i + 1);

			bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult.Should().BeTrue();
		}

		// Act
		PersistenceAggregate? result = await context.EventStore.FirstOrDefaultAsync(m => m.IncrementInt32 == matchingIncrement, orderByClause: m => m.OrderBy(p => p.Int32Value), cancellationToken: tokenSource.Token);

		// Assert
		result.Should().BeNull();
		result!.Int32Value.Should().Be(1);
	}

	[Fact]
	public async Task FirstOrDefaultAsync_GivenMultipleMatchingAggregates_ShouldNotThrowException()
	{
		const int aggregateCount = 10;
		const int matchingIncrement = 10;

		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext();

		for (int i = 0; i < aggregateCount; i++)
		{
			PersistenceAggregate aggregate = CreateAggregate();
			for (int x = 0; x < matchingIncrement; x++)
			{
				aggregate.IncrementInt32Value();
			}

			bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult.Should().BeTrue();
		}

		// Act
		Func<Task> func = async () => await context.EventStore.FirstOrDefaultAsync(m => m.IncrementInt32 == matchingIncrement, cancellationToken: tokenSource.Token);

		// Assert
		await func.Should().NotThrowAsync();
	}

	[Fact]
	public async Task FirstOrDefaultAsync_GivenMultipleMatchingAggregates_ShouldNotReturnNull()
	{
		const int matchingIncrement = 10;

		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext();

		for (int i = 0; i < 10; i++)
		{
			PersistenceAggregate aggregate = CreateAggregate();
			for (int x = 0; x < matchingIncrement; x++)
			{
				aggregate.IncrementInt32Value();
			}

			bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult.Should().BeTrue();
		}

		// Act
		PersistenceAggregate? result = await context.EventStore.FirstOrDefaultAsync(m => m.IncrementInt32 == matchingIncrement, cancellationToken: tokenSource.Token);

		// Assert
		result?.Should().NotBeNull();
	}

	[Fact]
	public async Task FirstOrDefaultAsync_GivenSingleMatchingAggregates_ReturnsAggregate()
	{
		const int matchingIncrement = 10;

		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
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
		PersistenceAggregate? result = await context.EventStore.FirstOrDefaultAsync(m => m.IncrementInt32 == matchingIncrement, cancellationToken: tokenSource.Token);

		// Assert
		result?.Id().Should().Be(aggregateId);
	}

	[Fact]
	public async Task FirstOrDefaultAsync_GivenNoMatchingAggregates_ReturnsNull()
	{
		const int matchingIncrement = 10;

		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext();

		for (int i = 0; i < 10; i++)
		{
			PersistenceAggregate aggregate = CreateAggregate();
			for (int x = 0; x < matchingIncrement; x++)
			{
				aggregate.IncrementInt32Value();
			}

			bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult.Should().BeTrue();
		}

		// Act
		PersistenceAggregate? result = await context.EventStore.FirstOrDefaultAsync(m => m.IncrementInt32 == -1, cancellationToken: tokenSource.Token);

		// Assert
		result.Should().BeNull();
	}
}
