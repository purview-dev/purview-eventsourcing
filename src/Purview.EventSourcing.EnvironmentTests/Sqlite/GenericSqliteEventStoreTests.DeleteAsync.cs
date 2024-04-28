using Purview.Testing;

namespace Purview.EventSourcing.Sqlite;

partial class GenericSqliteEventStoreTests<TAggregate>
{
	public async Task DeleteAsync_GivenPreviouslySavedAggregate_MarksAsDeleted()
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
	}
}
