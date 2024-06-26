﻿using Microsoft.Azure.Cosmos;
using Purview.EventSourcing.Aggregates.Persistence;

namespace Purview.EventSourcing.CosmosDb.Snapshot;

partial class CosmosDbSnapshotEventStoreTests
{
	[Fact]
	public async Task SaveAsync_GivenNewAggregateWithChanges_SavesAggregate()
	{
		// Arrange
		using var tokenSource = TestHelpers.CancellationTokenSource();
		await using var context = fixture.CreateContext();

		var aggregateId = Guid.NewGuid().ToString();
		var aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		PartitionKey partitionKey = new(aggregate.AggregateType);

		// Act
		bool result = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();

		aggregate
			.IsNew()
			.Should()
			.BeFalse();

		// Verify by re-getting the aggregate, knowing that the cache is disabled.
		var aggregateFromCosmosDb = await context.CosmosDbClient.GetAsync<PersistenceAggregate>(aggregateId, partitionKey, cancellationToken: tokenSource.Token);

		aggregateFromCosmosDb.Should().NotBeNull();

		aggregateFromCosmosDb?.Id().Should().Be(aggregate.Id());

		aggregateFromCosmosDb?.IncrementInt32.Should().Be(aggregate.IncrementInt32);

		aggregateFromCosmosDb?.Details.SavedVersion.Should().Be(aggregate.Details.SavedVersion);

		aggregateFromCosmosDb?.Details.CurrentVersion.Should().Be(aggregate.Details.CurrentVersion);

		aggregateFromCosmosDb?.Details.SnapshotVersion.Should().Be(aggregate.Details.SnapshotVersion);

		aggregateFromCosmosDb?.Details.Etag.Should().Be(aggregate.Details.Etag);
	}
}
