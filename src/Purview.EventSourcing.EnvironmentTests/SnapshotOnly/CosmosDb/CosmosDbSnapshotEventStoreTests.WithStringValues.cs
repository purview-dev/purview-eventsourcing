using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Primitives;
using Purview.EventSourcing.Aggregates.Persistence;
using Purview.EventSourcing.SnapshotOnly.CosmosDb;

namespace Purview.EventSourcing.CosmosDb.Snapshot;

partial class CosmosDbSnapshotEventStoreTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(10)]
	public async Task CanQuery_GivenAggregatesContainsDictionaryWithStringValuesAsValue_QueryAsExpected(int numberOfAggregates)
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext(correlationIdsToGenerate: numberOfAggregates);

		string aggregateType = CreateAggregate().AggregateType;
		PartitionKey partitionKey = new(aggregateType);

		for (int aggregateIndex = 0; aggregateIndex < numberOfAggregates; aggregateIndex++)
		{
			PersistenceAggregate aggregate = CreateAggregate($"{aggregateIndex}_{context.RunId}");
			aggregate.AddKVPs(new[] {
							new KeyValuePair<string, StringValues> ( "name-1", "value-1" ),
							new KeyValuePair<string, StringValues> ( "name-2", new[] { "value-100", "value-200" })
						});

			bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult
				.Should()
				.BeTrue();
		}

		// Act
		IEnumerable<PersistenceAggregate> aggregates = (await context.CosmosDbClient.QueryAsync<PersistenceAggregate>(
			m => m.StringValuesDictionary["name-1"] == "value-1",
			partitionKey,
			maxRecords: numberOfAggregates,
			cancellationToken: tokenSource.Token))
			.Results;

		// Assert
		aggregates
			.Should()
			.HaveCount(numberOfAggregates);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(10)]
	public async Task CanQuery_GivenAggregatesContainsDictionaryWithStringsAsValues_QueryAsExpected(int numberOfAggregates)
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();
		await using CosmosDbSnapshotEventStoreContext context = fixture.CreateContext(correlationIdsToGenerate: numberOfAggregates);

		string aggregateType = CreateAggregate().AggregateType;
		PartitionKey partitionKey = new(aggregateType);

		for (int aggregateIndex = 0; aggregateIndex < numberOfAggregates; aggregateIndex++)
		{
			PersistenceAggregate aggregate = CreateAggregate($"{aggregateIndex}_{context.RunId}");
			aggregate.AddKVPs(new[] {
							new KeyValuePair<string, string> ( "name-1", "value-1" ),
							new KeyValuePair<string, string> ( "name-2", "value-100" )
						});

			bool saveResult = await context.EventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			saveResult.Should().BeTrue();
		}

		// Act
#pragma warning disable CA1304 // Specify CultureInfo
		IEnumerable<PersistenceAggregate> aggregates = (await context.CosmosDbClient.QueryAsync<PersistenceAggregate>(m => m.StringsDictionary["name-1"].ToLower() == "value-1", partitionKey, maxRecords: numberOfAggregates, cancellationToken: tokenSource.Token)).Results;
#pragma warning restore CA1304 // Specify CultureInfo

		// Assert
		aggregates.Should().HaveCount(numberOfAggregates);
	}
}
