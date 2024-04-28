namespace Purview.EventSourcing.AzureStorage.Table;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task GetOrCreateAsync_GivenAggregateDoesNotExist_CreatesNewAggregate()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		TAggregate? result = await eventStore.GetOrCreateAsync(aggregateId, cancellationToken: tokenSource.Token);

		// Assert
		result?
			.Should()
			.NotBeNull();

		result?
			.Id()
			.Should()
			.Be(aggregateId);

		result?
			.IsNew()
			.Should()
			.BeTrue();
	}
}
