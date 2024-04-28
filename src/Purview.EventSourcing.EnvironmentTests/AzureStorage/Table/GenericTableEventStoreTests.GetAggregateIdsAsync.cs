namespace Purview.EventSourcing.AzureStorage.Table;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task GetAggregateIdsAsync_GivenNAggregatesInTheStore_CorrectlyReturnsTheirIds(int aggregateCount)
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		List<string> generatedIds = [];
		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: aggregateCount);

		for (int i = 0; i < aggregateCount; i++)
		{
			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			generatedIds.Add(aggregateId);
		}

		// Act
		IEnumerable<string> returnedTypes = eventStore.GetAggregateIdsAsync(true, cancellationToken: tokenSource.Token).ToBlockingEnumerable(tokenSource.Token);

		// Assert
		returnedTypes
			.Should()
			.HaveCount(aggregateCount);

		generatedIds
			.Should()
			.BeEquivalentTo(returnedTypes);
	}

	public async Task GetAggregateIdsAsync_GivenNonDeletedAggregatesAndDeletedAggregatesInTheStoreAndRequestingOnlyNonDeleted_CorrectlyReturnsNonDeletedIdsOnly(int nonDeletedAggregateIdCount, int deletedAggregateIdCount)
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		List<string> generatedIds = [];
		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: nonDeletedAggregateIdCount + (deletedAggregateIdCount * 2));

		for (int i = 0; i < nonDeletedAggregateIdCount; i++)
		{
			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			generatedIds.Add(aggregateId);
		}

		for (int i = 0; i < deletedAggregateIdCount; i++)
		{
			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
			await eventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);
		}

		// Act
		IEnumerable<string> returnedTypes = eventStore.GetAggregateIdsAsync(false, cancellationToken: tokenSource.Token).ToBlockingEnumerable(tokenSource.Token);

		// Assert
		returnedTypes
			.Should()
			.HaveCount(nonDeletedAggregateIdCount);

		generatedIds
			.Should()
			.BeEquivalentTo(returnedTypes);
	}

	public async Task GetAggregateIdsAsync_GivenNonDeletedAggregatesAndDeletedAggregatesInTheStoreAndRequestingAll_CorrectlyReturnsAllIds(int nonDeletedAggregateIdCount, int deletedAggregateIdCount)
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		List<string> generatedIds = [];
		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: nonDeletedAggregateIdCount + (deletedAggregateIdCount * 2));

		for (int i = 0; i < nonDeletedAggregateIdCount; i++)
		{
			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			generatedIds.Add(aggregateId);
		}

		for (int i = 0; i < deletedAggregateIdCount; i++)
		{
			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
			await eventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);

			generatedIds.Add(aggregateId);
		}

		// Act
		IEnumerable<string> returnedTypes = eventStore.GetAggregateIdsAsync(true, cancellationToken: tokenSource.Token).ToBlockingEnumerable(tokenSource.Token);

		// Assert
		returnedTypes
			.Should()
			.HaveCount(deletedAggregateIdCount + nonDeletedAggregateIdCount);

		generatedIds
			.Should()
			.BeEquivalentTo(returnedTypes);
	}
}
