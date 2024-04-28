using Purview.Testing;

namespace Purview.EventSourcing.Sqlite;

partial class GenericSqliteEventStoreTests<TAggregate>
{
	public async Task GetDeletedAsync_GivenDeletedAggregate_ReturnsAggregate()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			SqliteEventStore<TAggregate> eventStore = CreateEventStore(correlationIdsToGenerate: 2);

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
}
