using Purview.EventSourcing.Aggregates.Persistence.Events;
using Purview.EventSourcing.AzureStorage.Table.Entities;
using Purview.EventSourcing.AzureStorage.Table.Exceptions;
using Purview.Interfaces.Storage.AzureStorage.Table;

namespace Purview.EventSourcing.AzureStorage.Table;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task GetAsync_GivenAggregateIsDeletedAndDeletedModeIsSetToThrow_ThrowsEventStoreAggregateDeletedException()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

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
			.ThrowExactlyAsync<EventStoreAggregateIsDeletedException>();
	}

	public async Task GetAsync_GivenAnAggregateWithSavedEventsButNoSnapshot_RecreatesAggregate(int eventsToCreate)
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		for (int i = 0; i < eventsToCreate; i++)
		{
			aggregate.IncrementInt32Value();
		}

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Act
		string blobName = eventStore.GenerateSnapshotBlobName(aggregateId);
		bool exists = await fixture.BlobClient.ExistsAsync(blobName, cancellationToken: tokenSource.Token);

		exists
			.Should()
			.BeTrue();

		await fixture.BlobClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: tokenSource.Token);

		exists = await fixture.BlobClient.ExistsAsync(blobName, cancellationToken: tokenSource.Token);

		exists
			.Should()
			.BeFalse();

		// Assert
		TAggregate? result = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);

		result
			.Should()
			.NotBeNull();

		result!
			.IsNew()
			.Should()
			.BeFalse();

		result!.Id()
			.Should()
			.Be(aggregate.Id());

		result!.IncrementInt32
			.Should()
			.Be(aggregate.IncrementInt32);

		result!.Details.SavedVersion
			.Should()
			.Be(aggregate.Details.SavedVersion);

		result!.Details.CurrentVersion
			.Should()
			.Be(aggregate.Details.CurrentVersion);

		result!.Details.Etag
			.Should()
			.Be(aggregate.Details.Etag);

		result!.Details.SnapshotVersion
			.Should()
			.Be(0, "There is no snapshot version as it was deleted as part of this test.");
	}

	public async Task GetAsync_GivenAnAggregateWithMoreEventsThanTheSnapshot_RecreatesAggregate(int eventsToCreate)
	{
		// Arrange
		const int snapshotInterval = 5;
		const int eventCountOffset = snapshotInterval - 1;

		int expectedSnapshotVersion = eventsToCreate - eventCountOffset;
		int initialEventsToCreate = eventsToCreate - eventCountOffset;

		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>(snapshotRecalculationInterval: snapshotInterval);

		// Act
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		for (int i = 0; i < initialEventsToCreate; i++)
		{
			aggregate.IncrementInt32Value();
		}

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		for (int i = 0; i < eventCountOffset; i++)
		{
			aggregate.IncrementInt32Value();
		}

		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		TAggregate? result = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);

		result
			.Should()
			.NotBeNull();

		result!
			.IsNew()
			.Should()
			.BeFalse();

		result!
			.IncrementInt32
			.Should()
			.Be(eventsToCreate);

		result!
			.Details
			.SavedVersion
			.Should()
			.Be(eventsToCreate);

		result!
			.Details
			.SnapshotVersion
			.Should()
			.Be(expectedSnapshotVersion);
	}

	// This is testing that the aggregate is still correct after having an event type removed (in this case,
	// it deserializes, but it's not registered any longer),
	// this is often due to the schema changes and the event not being required anymore, but the
	// event record still (correctly) exists.
	public async Task GetAsync_GivenAnAggregateWithNonRegisteredEventType_RecreatesAggregateAndLogsCannotApplyEvent(int eventsToCreate, int numberOfOldEventsToCreate)
	{
		// Arrange
		int totalEvents = eventsToCreate + numberOfOldEventsToCreate;

		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		// Register the event type here...!
		aggregate.RegisterOldEventType();

		for (int i = 0; i < eventsToCreate; i++)
		{
			aggregate.IncrementInt32Value();
		}

		for (int i = 0; i < numberOfOldEventsToCreate; i++)
		{
			aggregate.SetOldEventValue(Guid.NewGuid());
		}

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Get without using the snapshot, just from the event record.
		TAggregate? result = await eventStore.GetAsync(aggregateId, new EventStoreOperationContext
		{
			SkipSnapshot = true
		}, cancellationToken: tokenSource.Token);

		// Assert
		fixture
			.Logs
			.Received(numberOfOldEventsToCreate)
			.CannotApplyEvent(aggregateId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Is<string>(eventType => eventType.Contains(typeof(OldEvent).Name, StringComparison.Ordinal)), Arg.Any<int>());

		result
			.Should()
			.NotBeNull();

		result!
			.IsNew()
			.Should()
			.BeFalse();

		result!.Id()
			.Should()
			.Be(aggregate.Id());

		result!
			.IncrementInt32
			.Should()
			.Be(aggregate.IncrementInt32);

		result!
			.Details.SavedVersion
			.Should()
			.Be(totalEvents);

		result!
			.Details.CurrentVersion
			.Should()
			.Be(totalEvents);
	}

	// This is testing that the aggregate is still correct after an event type cannot be found - removed 
	// from the assembly/ failure to load the type -
	// this is often due to the schema changes and the event not being required anymore, but the
	// event record still (correctly) exists.
	public async Task GetAsync_GivenAnAggregateWithUnknownEventType_RecreatesAggregateAndLogsUnknown(int eventsToCreate, int numberOfOldEventsToCreate)
	{
		// Arrange
		const string unknownEventType = "an-unknown-type";

		int totalEvents = eventsToCreate + numberOfOldEventsToCreate;

		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		// Register the event type here...!
		aggregate.RegisterOldEventType();

		for (int i = 0; i < eventsToCreate; i++)
		{
			aggregate.IncrementInt32Value();
		}

		for (int i = 0; i < numberOfOldEventsToCreate; i++)
		{
			aggregate.SetOldEventValue(Guid.NewGuid());
		}

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Update existing events to make them unknown types effectively.
		IAsyncEnumerable<EventEntity> eventsToUpdate = eventStore.GetEventRangeEntitiesAsync(aggregateId, eventsToCreate + 1, totalEvents, tokenSource.Token);

		BatchOperation batchOperation = new();
		BatchOperation batch = batchOperation;
		await foreach (EventEntity eventToUpdate in eventsToUpdate)
		{
			eventToUpdate.EventType = unknownEventType;

			batch.Update(eventToUpdate, merge: false);
		}

		await fixture.TableClient.SubmitBatchAsync(batch, tokenSource.Token);

		// Get without using the snapshot, just from the event record.
		TAggregate? result = await eventStore.GetAsync(aggregateId, new EventStoreOperationContext
		{
			SkipSnapshot = true
		}, cancellationToken: tokenSource.Token);

		// Assert
		fixture
			.Logs
			.Received(numberOfOldEventsToCreate)
			.SkippedUnknownEvent(aggregateId, Arg.Any<string>(), Arg.Any<string>(), unknownEventType, Arg.Any<int>());

		result
			.Should()
			.NotBeNull();

		result!
			.IsNew()
			.Should()
			.BeFalse();

		result!.Id()
			.Should()
			.Be(aggregate.Id());

		result!
			.IncrementInt32
			.Should()
			.Be(aggregate.IncrementInt32);

		result!
			.Details.SavedVersion
			.Should()
			.Be(totalEvents);

		result!
			.Details.CurrentVersion
			.Should()
			.Be(totalEvents);
	}
}
