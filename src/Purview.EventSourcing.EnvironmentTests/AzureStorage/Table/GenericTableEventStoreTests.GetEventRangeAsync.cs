using Purview.EventSourcing.Interfaces.Aggregates.Events;

namespace Purview.EventSourcing.AzureStorage.Table;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task GetEventRangeAsync_GivenARequestedRangeOfEvents_GetsEventsRequested(int eventsToCreate, int startEvent, int? endEvent, int expectedEventCount)
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		TAggregate aggregate = CreateAggregate(id: aggregateId);
		for (int i = 0; i < eventsToCreate; i++)
		{
			aggregate.IncrementInt32Value();
		}

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Act
		IAsyncEnumerable<(IEvent @event, string eventType)> results = eventStore.GetEventRangeAsync(aggregateId, startEvent, endEvent, cancellationToken: tokenSource.Token);

		// Assert
		List<IEvent> eventList = [];
		await foreach ((IEvent @event, string _) in results)
		{
			eventList.Add(@event);
		}

		eventList
			.Should()
			.HaveCount(expectedEventCount);
	}

	public async Task GetEventRangeAsync_GivenARequestedRangeOfEvents_EventsAreReturnsInCorrectOrder(int eventsToCreate, int startEvent, int? endEvent)
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		TAggregate aggregate = CreateAggregate(id: aggregateId);
		for (int i = 0; i < eventsToCreate; i++)
		{
			aggregate.IncrementInt32Value();
		}

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Act
		IAsyncEnumerable<(IEvent @event, string eventType)> results = eventStore.GetEventRangeAsync(aggregateId, startEvent, endEvent, cancellationToken: tokenSource.Token);

		// Assert
		List<IEvent> eventList = [];
		await foreach ((IEvent @event, string _) in results)
		{
			@event
				.Details
				.AggregateVersion
				.Should()
				.Be(startEvent);

			startEvent++;
		}
	}
}
