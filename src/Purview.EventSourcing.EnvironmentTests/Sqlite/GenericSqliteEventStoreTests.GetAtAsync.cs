﻿using Purview.Testing;

namespace Purview.EventSourcing.Sqlite;

partial class GenericSqliteEventStoreTests<TAggregate>
{
	public async Task GetAtAsync_GivenAnAggregateWithSavedEvents_RecreatesAggregateToPreviousVersion(int previousEventsToCreate)
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);
			for (int i = 0; i < previousEventsToCreate; i++)
			{
				aggregate.IncrementInt32Value();
			}

			SqliteEventStore<TAggregate> eventStore = CreateEventStore();

			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			// Act

			// Add an extra event to push it past the requested number of events.
			aggregate.IncrementInt32Value();
			await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			aggregate
				.IncrementInt32
				.Should()
				.Be(previousEventsToCreate + 1);

			// Assert
			TAggregate? result = await eventStore.GetAtAsync(aggregateId, version: previousEventsToCreate, cancellationToken: tokenSource.Token);

			result
				.Should()
				.NotBeNull();

			result?
				.IncrementInt32
				.Should()
				.Be(previousEventsToCreate);

			result?
				.Details
				.CurrentVersion
				.Should()
				.Be(previousEventsToCreate);

			result?
				.Details
				.Locked
				.Should()
				.BeTrue();
		}
	}
}
