﻿namespace Purview.EventSourcing.MongoDB;

partial class GenericMongoDBEventStoreTests<TAggregate>
{
	public async Task IsDeletedAsync_GivenDeletedAggregates_ReturnsTrue()
	{
		// Arrange
		using var tokenSource = TestHelpers.CancellationTokenSource();

		var aggregateId = $"{Guid.NewGuid()}";
		var aggregate = TestHelpers.Aggregate<TAggregate>(aggregateId: aggregateId); ;
		aggregate.IncrementInt32Value();

		var eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: 2);

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
		await eventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);

		// Act
		var result = await eventStore.IsDeletedAsync(aggregateId, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	public async Task IsDeletedAsync_GivenNonDeletedAggregates_ReturnsFalse()
	{
		// Arrange
		using var tokenSource = TestHelpers.CancellationTokenSource();

		var aggregateId = $"{Guid.NewGuid()}";
		var aggregate = TestHelpers.Aggregate<TAggregate>(aggregateId: aggregateId); ;
		aggregate.IncrementInt32Value();

		var eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: 2);

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Act
		var result = await eventStore.IsDeletedAsync(aggregateId, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeFalse();
	}
}
