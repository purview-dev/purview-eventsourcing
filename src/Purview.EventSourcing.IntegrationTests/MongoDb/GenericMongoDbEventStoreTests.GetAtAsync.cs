namespace Purview.EventSourcing.MongoDb;

partial class GenericMongoDbEventStoreTests<TAggregate>
{
	public async Task GetAtAsync_GivenAnAggregateWithSavedEvents_RecreatesAggregateToPreviousVersion(int previousEventsToCreate)
	{
		// Arrange
		using var tokenSource = TestHelpers.CancellationTokenSource();

		var aggregateId = $"{Guid.NewGuid()}";
		var aggregate = TestHelpers.Aggregate<TAggregate>(aggregateId: aggregateId); ;
		for (var i = 0; i < previousEventsToCreate; i++)
		{
			aggregate.IncrementInt32Value();
		}

		var eventStore = fixture.CreateEventStore<TAggregate>();

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Act

		// Add an extra event to push it past the requested number of events.
		aggregate.IncrementInt32Value();
		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		aggregate
			.IncrementInt32
			.Should()
			.Be(previousEventsToCreate + 1);

		// Assert
		var result = await eventStore.GetAtAsync(aggregateId, version: previousEventsToCreate, cancellationToken: tokenSource.Token);

		result
			.Should()
			.NotBeNull();

		result?
			.IncrementInt32
			.Should()
			.Be(previousEventsToCreate);

		result?
			.Details
			.SavedVersion
			.Should()
			.Be(previousEventsToCreate);

		result?
			.Details
			.CurrentVersion
			.Should()
			.Be(previousEventsToCreate);

		result?
			.Details
			.Locked
			.Should()
			.BeTrue();
	}
}
