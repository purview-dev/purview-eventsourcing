namespace Purview.EventSourcing.AzureStorage;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task GetOrCreateAsync_GivenAggregateDoesNotExist_CreatesNewAggregate()
	{
		// Arrange
		using var tokenSource = TestHelpers.CancellationTokenSource();

		var aggregateId = $"{Guid.NewGuid()}";
		var eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		var result = await eventStore.GetOrCreateAsync(aggregateId, cancellationToken: tokenSource.Token);

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
