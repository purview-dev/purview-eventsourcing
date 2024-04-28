using Purview.EventSourcing.Interfaces.ChangeFeed;

namespace Purview.EventSourcing.AzureStorage.Table;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task DeleteAsync_GivenPreviouslySavedAggregate_MarksAsDeleted()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: 2);

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		TAggregate? aggregateResult = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token) ?? throw new NullReferenceException();

		// Act
		bool result = await eventStore.DeleteAsync(aggregateResult, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();

		aggregateResult
			.Details
			.IsDeleted
			.Should()
			.BeTrue();

		aggregateResult
			.Details
			.SavedVersion
			.Should()
			.Be(2);
	}

	public async Task DeleteAsync_WhenTableStoreConfigRemoveDeletedFromCacheIsTrueAndPreviouslySavedAggregate_RemovesFromCache()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(correlationIdsToGenerate: 2, removeFromCacheOnDelete: true);

		string cacheKey = eventStore.CreateCacheKey(aggregateId);

		await eventStore.SaveAsync(aggregate, tokenSource.Token);

		TAggregate? aggregateResult = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token) ?? throw new NullReferenceException();

		// Act
		bool result = await eventStore.DeleteAsync(aggregateResult, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();

		await fixture.Cache
			.Received(1)
			.RemoveAsync(cacheKey, Arg.Any<CancellationToken>());
	}

	public async Task DeleteAsync_GivenDelete_NotifiesChangeFeed()
	{
		// Arrange
		IAggregateChangeFeedNotifier<TAggregate> aggregateChangeNotifier = Substitute.For<IAggregateChangeFeedNotifier<TAggregate>>();

		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		bool beforeWasCalled = false;
		bool afterWasCalled = false;
		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(aggregateChangeNotifier: aggregateChangeNotifier);

		aggregateChangeNotifier
			.When(m => m.BeforeDeleteAsync(aggregate, Arg.Any<CancellationToken>()))
			.Do(_ => beforeWasCalled = true);

		aggregateChangeNotifier
			.When(m => m.AfterDeleteAsync(aggregate, Arg.Any<CancellationToken>()))
			.Do(_ => afterWasCalled = true);

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Act
		bool result = await eventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		beforeWasCalled
			.Should()
			.BeTrue();

		afterWasCalled
			.Should()
			.BeTrue();

		await aggregateChangeNotifier
			.Received(1)
			.BeforeDeleteAsync(aggregate, Arg.Any<CancellationToken>());

		await aggregateChangeNotifier
			.Received(1)
			.AfterDeleteAsync(aggregate, Arg.Any<CancellationToken>());
	}
}
