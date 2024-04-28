using Purview.Testing;

namespace Purview.EventSourcing.Sqlite;

partial class GenericSqliteEventStoreTests<TAggregate>
{
	public async Task GetOrCreateAsync_GivenAggregateDoesNotExist_CreatesNewAggregate()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			string aggregateId = $"{Guid.NewGuid()}";
			SqliteEventStore<TAggregate> eventStore = CreateEventStore();

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
}
