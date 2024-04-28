using Purview.Testing;

namespace Purview.EventSourcing.Sqlite;

partial class GenericSqliteEventStoreTests<TAggregate>
{
	public async Task IsDeletedAsync_GivenDeletedAggregates_ReturnsTrue()
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
			bool result = await eventStore.IsDeletedAsync(aggregateId, cancellationToken: tokenSource.Token);

			// Assert
			result
				.Should()
				.BeTrue();
		}
	}

	public async Task IsDeletedAsync_GivenNonDeletedAggregates_ReturnsFalse()
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

			// Act
			bool result = await eventStore.IsDeletedAsync(aggregateId, cancellationToken: tokenSource.Token);

			// Assert
			result
				.Should()
				.BeFalse();
		}
	}
}
