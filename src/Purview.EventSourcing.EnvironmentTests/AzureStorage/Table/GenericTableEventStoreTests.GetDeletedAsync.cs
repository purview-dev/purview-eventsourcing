namespace Purview.EventSourcing.AzureStorage.Table;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task GetDeletedAsync_GivenDeletedAggregate_ReturnsAggregate()
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
		TAggregate? aggregateResult = await eventStore.GetDeletedAsync(aggregateId, cancellationToken: tokenSource.Token);

		// Assert
		aggregateResult!
			.Details
			.IsDeleted
			.Should()
			.BeTrue();

		aggregateResult!
			.Details
			.SavedVersion
			.Should()
			.Be(2);
	}
}
