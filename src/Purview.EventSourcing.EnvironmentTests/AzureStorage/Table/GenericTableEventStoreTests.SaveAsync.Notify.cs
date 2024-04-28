using Purview.EventSourcing.Interfaces.Aggregates.Events;
using Purview.EventSourcing.Interfaces.ChangeFeed;

namespace Purview.EventSourcing.AzureStorage.Table;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task SaveAsync_GivenAggregateWithChanges_NotifiesChangeFeed(int eventsToCreate)
	{
		// Arrange
		IAggregateChangeFeedNotifier<TAggregate> aggregateChangeNotifier = Substitute.For<IAggregateChangeFeedNotifier<TAggregate>>();

		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		bool beforeWasCalled = false;
		bool afterWasCalled = false;
		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		for (int i = 0; i < eventsToCreate; i++)
		{
			aggregate.AppendString($"{i + 1} of {eventsToCreate}(s) to created.");
		}

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(aggregateChangeNotifier: aggregateChangeNotifier);

		aggregateChangeNotifier
			.When(m => m.BeforeSaveAsync(Arg.Is(aggregate), Arg.Is(true), Arg.Any<CancellationToken>()))
			.Do(callInfo =>
			{
				TAggregate a = callInfo.ArgAt<TAggregate>(0);
				a.AppendString(nameof(aggregateChangeNotifier.AfterSaveAsync));

				beforeWasCalled = true;
			});

		aggregateChangeNotifier
			.When(m => m.AfterSaveAsync(Arg.Is(aggregate), Arg.Is(0), Arg.Is(true), Arg.Any<IEvent[]>(), Arg.Any<CancellationToken>()))
			.Do(_ => afterWasCalled = true);

		// Act
		bool result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		beforeWasCalled
			.Should()
			.BeTrue();

		afterWasCalled
			.Should()
			.BeTrue();

		await aggregateChangeNotifier
			.Received(1)
			.BeforeSaveAsync(aggregate, true, Arg.Any<CancellationToken>());

		await aggregateChangeNotifier
			.Received(1)
			.AfterSaveAsync(aggregate, 0, true, Arg.Is<IEvent[]>(events => events.Length == eventsToCreate), Arg.Any<CancellationToken>());
	}

	public async Task SaveAsync_GivenAggregateWithNoChanges_DoesNotNotifyChangeFeed()
	{
		// Arrange
		IAggregateChangeFeedNotifier<TAggregate> aggregateChangeNotifier = Substitute.For<IAggregateChangeFeedNotifier<TAggregate>>();

		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(aggregateChangeNotifier: aggregateChangeNotifier);

		// Act
		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		await aggregateChangeNotifier
			.DidNotReceive()
			.AfterSaveAsync(aggregate, 0, true, Arg.Any<IEvent[]>(), Arg.Any<CancellationToken>());
	}
}
