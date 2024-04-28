using System.Text;
using Azure.Data.Tables;
using Purview.EventSourcing.Aggregates;
using Purview.EventSourcing.AzureStorage.Table.Entities;
using Purview.Interfaces.Storage.AzureStorage.Table;

namespace Purview.EventSourcing.AzureStorage.Table;

partial class GenericTableEventStoreTests<TAggregate>
{
	public async Task SaveAsync_GivenAggregateWithDataAnnotationsAndInvalidProperties_NoChangesAreMadeAndNotSaved()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId, a => a.SetValidatedProperty(-1));

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		SaveResult<TAggregate> result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		result.Saved.Should().BeFalse();
		result.IsValid.Should().BeFalse();
		((bool)result).Should().BeFalse();
		result.ValidationResult.Errors.Should().HaveCount(1);
		result.ValidationResult.Errors.Single().PropertyName.Should().Be(nameof(IAggregateTest.IncrementInt32));
	}

	public async Task SaveAsync_GivenAggregateWithComplexProperty_SavesEventWithComplexProperty()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		ComplexTestType complexProperty = CreateComplexTestType();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);

		aggregate.SetComplexProperty(complexProperty);

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		SaveResult<TAggregate> result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Act
		TAggregate? aggregateGetResult = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);

		// Assert
		aggregateGetResult
			.Should()
			.NotBeNull();

		aggregate
			.ComplexTestType
			.Should()
			.BeEquivalentTo(aggregateGetResult!.ComplexTestType);
	}

	public async Task SaveAsync_GivenAggregateWithNoChanges_DoesNotSave()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		bool result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeFalse();

		fixture
			.Logs
			.Received(1)
			.SaveContainedNoChanges(aggregateId, Arg.Any<string>(), Arg.Any<string>());
	}

	public async Task SaveAsync_GivenNewAggregateWithChanges_SavesAggregate()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		aggregate.IncrementInt32Value();

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		bool result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();

		aggregate
			.IsNew()
			.Should()
			.BeFalse();

		// Verify by re-getting the aggregate, knowing that the cache is disabled.
		TAggregate? aggregateFromEventStore = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);

		aggregateFromEventStore
			.Should()
			.NotBeNull();

		aggregateFromEventStore?.Id()
			.Should()
			.Be(aggregate.Id());

		aggregateFromEventStore?.IncrementInt32
			.Should()
			.Be(aggregate.IncrementInt32);

		aggregateFromEventStore?.Details.SavedVersion
			.Should()
			.Be(aggregate.Details.SavedVersion);

		aggregateFromEventStore?.Details.CurrentVersion
			.Should()
			.Be(aggregate.Details.CurrentVersion);

		aggregateFromEventStore?.Details.SnapshotVersion
			.Should()
			.Be(aggregate.Details.SnapshotVersion);

		aggregateFromEventStore?.Details.Etag
			.Should()
			.Be(aggregate.Details.Etag);
	}

	public async Task SaveAsync_GivenStreamVersionWithoutVersionSetWhenSaved_StreamVersionHasCorrectEvent(int eventsToGenerate)
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		for (int i = 0; i < eventsToGenerate; i++)
		{
			aggregate.IncrementInt32Value();
		}

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		bool result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Get and update stream version to remove the Version property.
		TableEntity? streamVersion = await fixture.TableClient.GetAsync<TableEntity>(aggregateId, TableEventStoreConstants.StreamVersionRowKey, cancellationToken: tokenSource.Token) ?? throw new NullReferenceException();
		int? streamVersionVersion = streamVersion[nameof(StreamVersionEntity.Version)] as int?;

		streamVersion.Remove(nameof(StreamVersionEntity.Version));

		await fixture.TableClient.OperationAsync(TableTransactionActionType.UpdateReplace, streamVersion, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();

		aggregate
			.IsNew()
			.Should()
			.BeFalse();

		// Verify by re-getting the aggregate, knowing that the cache is disabled.
		TAggregate? aggregateFromEventStore = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);

		aggregateFromEventStore
			.Should()
			.NotBeNull();

		aggregateFromEventStore?.Id()
			.Should()
			.Be(aggregate.Id());

		aggregateFromEventStore?.IncrementInt32
			.Should()
			.Be(aggregate.IncrementInt32);

		aggregateFromEventStore?.Details.SavedVersion
			.Should()
			.Be(aggregate.Details.SavedVersion);

		aggregateFromEventStore?.Details.CurrentVersion
			.Should()
			.Be(aggregate.Details.CurrentVersion);

		aggregateFromEventStore?.Details.SnapshotVersion
			.Should()
			.Be(aggregate.Details.SnapshotVersion);

		streamVersionVersion
			.Should()
			.Be(eventsToGenerate);
	}

	public async Task SaveAsync_GivenNewAggregateWithLargeChanges_SavesAggregateWithLargeEventRecord()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);

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

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		bool result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();

		aggregate
			.IsNew()
			.Should()
			.BeFalse();

		// Verify by re-getting the aggregate, knowing that the cache is disabled.
		TAggregate? aggregateFromEventStore = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);

		(aggregateFromEventStore?.StringProperty ?? string.Empty)
			.Length
			.Should()
			.Be(aggregate.StringProperty.Length);

		aggregateFromEventStore?
			.StringProperty
			.Should()
			.Be(aggregate.StringProperty);

		sizeIsLessThan32K = Encoding.UTF8.GetByteCount(aggregateFromEventStore?.StringProperty ?? string.Empty) < short.MaxValue;

		sizeIsLessThan32K
			.Should()
			.BeFalse();
	}

	public async Task SaveAsync_GivenNewAggregateWithLargeChangesAndNoSnapshot_ReadsAggregateFromEvents()
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);

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

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		bool result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Assert
		result
			.Should()
			.BeTrue();

		aggregate
			.IsNew()
			.Should()
			.BeFalse();

		// Delete the snapshow to ensure the events are replayed.
		string blobName = eventStore.GenerateSnapshotBlobName(aggregateId);
		bool deleteResult = await fixture.BlobClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: tokenSource.Token);

		deleteResult
			.Should()
			.BeTrue();

		// Verify by re-getting the aggregate, knowing that the cache is disabled.
		TAggregate? aggregateFromEventStore = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);

		(aggregateFromEventStore?.StringProperty ?? string.Empty)
			.Length
			.Should()
			.Be(aggregate.StringProperty.Length);

		aggregateFromEventStore?
			.StringProperty
			.Should()
			.Be(aggregate.StringProperty);

		sizeIsLessThan32K = Encoding.UTF8.GetByteCount(aggregateFromEventStore?.StringProperty ?? string.Empty) < short.MaxValue;

		sizeIsLessThan32K
			.Should()
			.BeFalse();
	}

	public async Task SaveAsync_GivenEventCountIsGreaterThanMaximumNumberOfAllowedInBatchOperation_BatchesEvents(int eventsToGenerate)
	{
		// Minus 2 is because we also add the idempotency marker and stream on the first batch.
		if (eventsToGenerate < (Storage.AzureStorage.Table.TableClient.MaximumBatchSize - 2))
		{
			$"'{eventsToGenerate}' should be greater than {Storage.AzureStorage.Table.TableClient.MaximumBatchSize}.".Should().BeNull();
		}

		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		for (int i = 0; i < eventsToGenerate; i++)
		{
			aggregate.IncrementInt32Value();
		}

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		bool result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Get and update stream version to remove the Version property.
		TableEntity? streamVersion = await fixture.TableClient.GetAsync<TableEntity>(aggregateId, TableEventStoreConstants.StreamVersionRowKey, cancellationToken: tokenSource.Token);

		streamVersion
			.Should()
			.NotBeNull();

		int? streamVersionVersion = streamVersion![nameof(StreamVersionEntity.Version)] as int?;

		// Assert
		result
			.Should()
			.BeTrue();

		aggregate
			.IsNew()
			.Should()
			.BeFalse();

		// Verify by re-getting the aggregate, knowing that the cache is disabled.
		TAggregate? aggregateFromEventStore = await eventStore.GetAsync(aggregateId, cancellationToken: tokenSource.Token);

		aggregateFromEventStore
			.Should()
			.NotBeNull();

		aggregateFromEventStore?.Id()
			.Should()
			.Be(aggregate.Id());

		aggregateFromEventStore?.IncrementInt32
			.Should()
			.Be(aggregate.IncrementInt32);

		aggregateFromEventStore?.Details.SavedVersion
			.Should()
			.Be(aggregate.Details.SavedVersion);

		aggregateFromEventStore?.Details.CurrentVersion
			.Should()
			.Be(aggregate.Details.CurrentVersion);

		aggregateFromEventStore?.Details.SnapshotVersion
			.Should()
			.Be(aggregate.Details.SnapshotVersion);

		streamVersionVersion
			.Should()
			.Be(eventsToGenerate);
	}

	public async Task SaveAsync_GivenEventCountIsGreaterThanMaximumNumberOfAllowedEventsInSaveOperation_ThrowsException(int eventsToGenerate)
	{
		// Arrange
		using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

		string aggregateId = $"{Guid.NewGuid()}";
		TAggregate aggregate = CreateAggregate(id: aggregateId);
		for (int i = 0; i < eventsToGenerate; i++)
		{
			aggregate.IncrementInt32Value();
		}

		TableEventStore<TAggregate> eventStore = fixture.CreateEventStore<TAggregate>();

		// Act
		Func<Task> func = async () => await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

		// Get and update stream version to remove the Version property.
		await func.Should().ThrowExactlyAsync<ArgumentOutOfRangeException>();
	}
}
