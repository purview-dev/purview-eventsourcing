using Purview.Testing;

namespace Purview.EventSourcing.Sqlite;

partial class GenericSqliteEventStoreTests<TAggregate>
{
	public async Task GetAsync_GivenAggregateIsDeletedAndDeletedModeIsSetToThrow_ThrowsEventStoreAggregateDeletedException()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			SqliteEventStore<TAggregate> eventStore = CreateEventStore();

			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
			await eventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);

			// Act
			Func<Task<TAggregate?>> func = () => eventStore.GetAsync(aggregateId, new EventStoreOperationContext
			{
				DeleteMode = DeleteHandlingMode.ThrowsException
			}, cancellationToken: tokenSource.Token);

			// Assert
			await func
				.Should()
				.ThrowExactlyAsync<Exception>();
		}
	}
}
