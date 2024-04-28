namespace Purview.EventSourcing.Sqlite;

[NCrunch.Framework.Category("Storage")]
[NCrunch.Framework.Category("Sqlite")]
public sealed partial class SqliteEventStoreTests
{
	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task DeleteAsync_GivenAggregateExistsWithLargeEvent_PermanentlyDeletesAllData(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.DeleteAsync_GivenAggregateExistsWithLargeEvent_PermanentlyDeletesAllData();
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task DeleteAsync_GivenAggregateExists_PermanentlyDeletesAllData(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.DeleteAsync_GivenAggregateExists_PermanentlyDeletesAllData();
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task DeleteAsync_GivenPreviouslySavedAggregate_MarksAsDeleted(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.DeleteAsync_GivenPreviouslySavedAggregate_MarksAsDeleted();
	}

	[Theory]
	[MemberData(nameof(SteppedCountTestData))]
	public async Task GetAggregateIdsAsync_GivenNAggregatesInTheStore_CorrectlyReturnsTheirIds(bool useInMemory, Type aggregateType, int aggregateCount)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.GetAggregateIdsAsync_GivenNAggregatesInTheStore_CorrectlyReturnsTheirIds(aggregateCount);
	}

	[Theory]
	[MemberData(nameof(SteppedAggregateCountWithDeletedAggregateIdCountTestData))]
	public async Task GetAggregateIdsAsync_GivenNonDeletedAggregatesAndDeletedAggregatesInTheStoreAndRequestingAll_CorrectlyReturnsAllIds(bool useInMemory, Type aggregateType, int nonDeletedAggregateIdCount, int deletedAggregateIdCount)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.GetAggregateIdsAsync_GivenNonDeletedAggregatesAndDeletedAggregatesInTheStoreAndRequestingAll_CorrectlyReturnsAllIds(nonDeletedAggregateIdCount, deletedAggregateIdCount);
	}

	[Theory]
	[MemberData(nameof(SteppedAggregateCountWithDeletedAggregateIdCountTestData))]
	public async Task GetAggregateIdsAsync_GivenNonDeletedAggregatesAndDeletedAggregatesInTheStoreAndRequestingOnlyNonDeleted_CorrectlyReturnsNonDeletedIdsOnly(bool useInMemory, Type aggregateType, int nonDeletedAggregateIdCount, int deletedAggregateIdCount)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.GetAggregateIdsAsync_GivenNonDeletedAggregatesAndDeletedAggregatesInTheStoreAndRequestingOnlyNonDeleted_CorrectlyReturnsNonDeletedIdsOnly(nonDeletedAggregateIdCount, deletedAggregateIdCount);
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task GetAsync_GivenAggregateIsDeletedAndDeletedModeIsSetToThrow_ThrowsEventStoreAggregateDeletedException(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.GetAsync_GivenAggregateIsDeletedAndDeletedModeIsSetToThrow_ThrowsEventStoreAggregateDeletedException();
	}

	[Theory]
	[MemberData(nameof(SteppedCountTestData))]
	public async Task GetAtAsync_GivenAnAggregateWithSavedEvents_RecreatesAggregateToPreviousVersion(bool useInMemory, Type aggregateType, int previousEventsToCreate)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.GetAtAsync_GivenAnAggregateWithSavedEvents_RecreatesAggregateToPreviousVersion(previousEventsToCreate);
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task GetDeletedAsync_GivenDeletedAggregate_ReturnsAggregate(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.GetDeletedAsync_GivenDeletedAggregate_ReturnsAggregate();
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task GetOrCreateAsync_GivenAggregateDoesNotExist_CreatesNewAggregate(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.GetOrCreateAsync_GivenAggregateDoesNotExist_CreatesNewAggregate();
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task IsDeletedAsync_GivenDeletedAggregates_ReturnsTrue(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.IsDeletedAsync_GivenDeletedAggregates_ReturnsTrue();
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task IsDeletedAsync_GivenNonDeletedAggregates_ReturnsFalse(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.IsDeletedAsync_GivenNonDeletedAggregates_ReturnsFalse();
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task RestoreAsync_GivenPreviouslySavedAndDeletedAggregate_MarksAsNotDeleted(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.RestoreAsync_GivenPreviouslySavedAndDeletedAggregate_MarksAsNotDeleted();
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task SaveAsync_GivenAggregateWithDataAnnotationsAndInvalidProperties_NoChangesAreMadeAndNotSaved(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.SaveAsync_GivenAggregateWithDataAnnotationsAndInvalidProperties_NoChangesAreMadeAndNotSaved();
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task SaveAsync_GivenAggregateWithNoChanges_DoesNotSave(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.SaveAsync_GivenAggregateWithNoChanges_DoesNotSave();
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task SaveAsync_GivenNewAggregateWithChanges_SavesAggregate(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.SaveAsync_GivenNewAggregateWithChanges_SavesAggregate();
	}

	[Theory]
	[MemberData(nameof(AggregateTypes))]
	public async Task SaveAsync_GivenAggregateWithComplexProperty_SavesEventWithComplexProperty(bool useInMemory, Type aggregateType)
	{
		ISqliteEventStoreTests sqliteEventStoreTests = CreateTableStoreTests(useInMemory, aggregateType);

		await sqliteEventStoreTests.SaveAsync_GivenAggregateWithComplexProperty_SavesEventWithComplexProperty();
	}
}
