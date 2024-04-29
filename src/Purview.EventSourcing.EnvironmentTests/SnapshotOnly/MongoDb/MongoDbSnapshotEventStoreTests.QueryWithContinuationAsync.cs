﻿using System.Linq.Expressions;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.MongoDb;
using Purview.Interfaces.Storage;

namespace Purview.EventSourcing.MongoDb.Snapshot;

partial class MongoDbSnapshotEventStoreTests
{
	[Theory]
	[InlineData(10, 5)]
	[InlineData(20, 5)]
	[InlineData(25, 5)]
	[InlineData(26, 5)]
	[InlineData(27, 5)]
	[InlineData(50, 5)]
	[InlineData(51, 5)]
	public async Task ListAsync_GivenData_ListsAsExpected(int numberOfAggregates, int pageCount)
	{
		const int numberOfEvents = 10;

		// Arrange
		using CancellationTokenSource tokenSource = TestHelpers.CancellationTokenSource();
		using MongoDbSnapshotTestContext context = fixture.CreateContext(correlationIdsToGenerate: numberOfAggregates);

		MongoDbSnapshotEventStore<PersistenceAggregate> eventStore = context.EventStore;

		for (int aggregateIndex = 0; aggregateIndex < numberOfAggregates; aggregateIndex++)
		{
			PersistenceAggregate aggregate = CreateAggregate($"{aggregateIndex}_{context.RunId}");

			for (int eventIndex = 0; eventIndex < numberOfEvents; eventIndex++)
			{
				aggregate.IncrementInt32Value();
			}

			bool saveResult = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult
				.Should()
				.BeTrue();
		}

		// Act
		List<PersistenceAggregate> aggregates = [];

		ContinuationResponse<PersistenceAggregate> aggregateResponse = await eventStore.ListAsync(maxRecordCount: pageCount, cancellationToken: tokenSource.Token);
		aggregates.AddRange(aggregateResponse.Results);

		while (aggregateResponse.ContinuationToken.HasValue())
		{
			aggregateResponse = await eventStore.ListAsync(aggregateResponse.ToRequest(), cancellationToken: tokenSource.Token);
			aggregates.AddRange(aggregateResponse.Results);
		}

		// Assert
		aggregates
			.Count
			.Should()
			.Be(numberOfAggregates);
	}

	[Theory]
	[InlineData(10, 5)]
	[InlineData(20, 5)]
	[InlineData(25, 5)]
	[InlineData(26, 5)]
	[InlineData(27, 5)]
	[InlineData(50, 5)]
	[InlineData(51, 5)]
	public async Task QueryAsync_GivenWhereClause_QueryAsExpected(int numberOfAggregates, int pageCount)
	{
		const int numberOfEvents = 10;

		// Arrange
		using MongoDbSnapshotTestContext context = fixture.CreateContext(correlationIdsToGenerate: numberOfAggregates * 2);
		MongoDbSnapshotEventStore<PersistenceAggregate> eventStore = context.EventStore;

		// These are matching.
		for (int aggregateIndex = 0; aggregateIndex < numberOfAggregates; aggregateIndex++)
		{
			PersistenceAggregate aggregate = CreateAggregate($"{aggregateIndex}_{context.RunId}");

			for (int eventIndex = 0; eventIndex < numberOfEvents; eventIndex++)
			{
				aggregate.IncrementInt32Value();
			}

			bool saveResult = await eventStore.SaveAsync(aggregate);

			saveResult
				.Should()
				.BeTrue();
		}

		// These are non-matching.
		for (int aggregateIndex = 0; aggregateIndex < numberOfAggregates; aggregateIndex++)
		{
			PersistenceAggregate aggregate = CreateAggregate($"{aggregateIndex + (numberOfAggregates + 100000)}_{context.RunId}");

			// We're changing the event count so as to make the query not match these updated records.
			for (int eventIndex = 0; eventIndex < (numberOfEvents * 2); eventIndex++)
			{
				aggregate.IncrementInt32Value();
			}

			bool saveResult = await eventStore.SaveAsync(aggregate);

			saveResult
				.Should()
				.BeTrue();
		}

		// Act
		List<PersistenceAggregate> aggregates = [];

		Expression<Func<PersistenceAggregate, bool>> query = a => a.IncrementInt32 == numberOfEvents;

		ContinuationResponse<PersistenceAggregate> aggregateResponse = await eventStore.QueryAsync(query, maxRecordCount: pageCount);
		aggregates.AddRange(aggregateResponse.Results);

		while (aggregateResponse.ContinuationToken.HasValue())
		{
			aggregateResponse = await eventStore.QueryAsync(query, aggregateResponse.ToRequest());
			aggregates.AddRange(aggregateResponse.Results);
		}

		// Assert
		aggregates
			.Count
			.Should()
			.Be(numberOfAggregates);
	}
}
