namespace Purview.EventSourcing.AzureStorage.Table;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task IsDeletedAsync_GivenDeletedAggregates_ReturnsTrue()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: 2);

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
		await eventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);

		// Act
		bool result = await eventStore.IsDeletedAsync(aggregateId, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	public async Task IsDeletedAsync_GivenNonDeletedAggregates_ReturnsFalse()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: 2);

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Act
		bool result = await eventStore.IsDeletedAsync(aggregateId, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeFalse();
	}
}
