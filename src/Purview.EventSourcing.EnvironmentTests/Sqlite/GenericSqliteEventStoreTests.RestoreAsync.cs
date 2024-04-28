using Purview.Testing;

namespace Purview.EventSourcing.Sqlite;

partial class GenericSqliteEventStoreTests<TAggregate>
{
	public async Task RestoreAsync_GivenPreviouslySavedAndDeletedAggregate_MarksAsNotDeleted()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate? aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			SqliteEventStore<TAggregate> eventStore = CreateEventStore(correlationIdsToGenerate: 3);

			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
			await eventStore.DeleteAsync(aggregate, cancellationToken: tokenSource.Token);

			aggregate = await eventStore.GetDeletedAsync(aggregateId);

			if (aggregate == null)
			{
				throw new NullReferenceException();
			}

			// Act
			bool result = await eventStore.RestoreAsync(aggregate, cancellationToken: tokenSource.Token);

			// Assert
			result
				.Should()
				.BeTrue();

			aggregate.Details.IsDeleted
				.Should()
				.BeFalse();

			aggregate.Details.SavedVersion
				.Should()
				.Be(3);
		}
	}
}
