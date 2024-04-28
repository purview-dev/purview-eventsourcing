using System.Text;
using Purview.EventSourcing.Aggregates;
using Purview.Testing;

namespace Purview.EventSourcing.Sqlite;

partial class GenericSqliteEventStoreTests<TAggregate>
{
	public async Task SaveAsync_GivenAggregateWithDataAnnotationsAndInvalidProperties_NoChangesAreMadeAndNotSaved()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);
			aggregate.SetValidatedProperty(-1);

			SqliteEventStore<TAggregate> eventStore = CreateEventStore();

			// Act
			SaveResult<TAggregate> result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			// Assert
			result
				.Saved
				.Should()
				.BeFalse();

			result
				.IsValid
				.Should()
				.BeFalse();

			((bool)result)
				.Should()
				.BeFalse();

			result
				.ValidationResult
				.Errors
				.Should()
				.HaveCount(1);

			result
				.ValidationResult
				.Errors
				.Single()
				.PropertyName
				.Should()
				.Be(nameof(IAggregateTest.IncrementInt32));
		}
	}

	public async Task SaveAsync_GivenAggregateWithComplexProperty_SavesEventWithComplexProperty()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			ComplexTestType complexProperty = CreateComplexTestType();

			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);

			aggregate.SetComplexProperty(complexProperty);

			SqliteEventStore<TAggregate> eventStore = CreateEventStore();

			bool saveResult = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);
			saveResult.Should().BeTrue();

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
	}

	public async Task SaveAsync_GivenAggregateWithNoChanges_DoesNotSave()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);

			SqliteEventStore<TAggregate> eventStore = CreateEventStore();

			// Act
			bool result = await eventStore.SaveAsync(aggregate, cancellationToken: tokenSource.Token);

			// Assert
			result.Should().BeFalse();
		}
	}

	public async Task SaveAsync_GivenNewAggregateWithChanges_SavesAggregate()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
		{
			// Arrange
			using CancellationTokenSource tokenSource = SubstituteBuilder.CreateCancellationTokenSource();

			string aggregateId = $"{Guid.NewGuid()}";
			TAggregate aggregate = CreateAggregate(id: aggregateId);
			aggregate.IncrementInt32Value();

			SqliteEventStore<TAggregate> eventStore = CreateEventStore();

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
	}

	public async Task SaveAsync_GivenNewAggregateWithLargeChanges_SavesAggregateWithLargeEventRecord()
	{
		await using (DisposeHelperAsync.New(DisposeAsyncCore))
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

			SqliteEventStore<TAggregate> eventStore = CreateEventStore();

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
	}
}
