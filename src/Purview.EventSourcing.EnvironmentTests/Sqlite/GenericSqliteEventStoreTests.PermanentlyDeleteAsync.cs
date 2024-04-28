using System.Text;
using Microsoft.EntityFrameworkCore;
using Purview.Testing;

namespace Purview.EventSourcing.Sqlite;

partial class GenericSqliteEventStoreTests<TAggregate>
{
	public async Task DeleteAsync_GivenAggregateExists_PermanentlyDeletesAllData()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate? aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			SqliteEventStore<TAggregate> eventStore = CreateEventStore(correlationIdsToGenerate: 2);

			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			aggregate = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);
			aggregate
				.Should()
				.NotBeNull();

			// Act
			bool result = await eventStore.DeleteAsync(aggregate!, new EventStoreOperationContext
			{
				PermanentlyDelete = true
			}, cancellationToken: tokenSource.Token);

			// Assert
			result
				.Should()
				.BeTrue();

			aggregate!
				.Details
				.IsDeleted
				.Should()
				.BeTrue();

			aggregate
				.Details
				.Locked
				.Should()
				.BeTrue();

			await ValidateEntitiesDeletedAsync(aggregate, eventStore, tokenSource.Token);
		}
	}

	public async Task DeleteAsync_GivenAggregateExistsWithLargeEvent_PermanentlyDeletesAllData()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate? aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			string value = string.Empty;
			bool sizeIsLessThan32K = true;
			while (sizeIsLessThan32K)
			{
				value += "abcdefghijklmnopqrstvwxyz";
				value += "ABCDEFGHIJKLMNOPQRSTVWXYZ";
				value += "1234567890";

				sizeIsLessThan32K = Encoding.UTF8.GetByteCount(value) < short.MaxValue;
			}

			aggregate.AppendString(value);

			SqliteEventStore<TAggregate> eventStore = CreateEventStore(correlationIdsToGenerate: 2);

			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			aggregate = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);

			if (aggregate == null)
			{
				throw new NullReferenceException();
			}

			// Act
			bool result = await eventStore.DeleteAsync(aggregate, new EventStoreOperationContext
			{
				PermanentlyDelete = true
			}, cancellationToken: tokenSource.Token);

			// Assert
			result
				.Should()
				.BeTrue();

			aggregate
				.Details
				.IsDeleted
				.Should()
				.BeTrue();

			aggregate
				.Details
				.Locked
				.Should()
				.BeTrue();

			await ValidateEntitiesDeletedAsync(aggregate, eventStore, tokenSource.Token);
		}
	}

	static async Task ValidateEntitiesDeletedAsync(TAggregate aggregate, SqliteEventStore<TAggregate> eventStore, CancellationToken cancellationToken)
	{
		Context.EventStoreContext context = await eventStore.GetContextAsync();

		Context.Entities.EventEntity[] results = await context.Events.Where(e => e.AggregateId == aggregate.Details.Id).ToArrayAsync(cancellationToken);
		results
			.Should()
			.BeEmpty();

		Context.Entities.StreamEntity? stream = await context.Streams.SingleOrDefaultAsync(s => s.AggregateId == aggregate.Details.Id, cancellationToken);

		stream
			.Should()
			.BeNull();
	}
}
