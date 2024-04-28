﻿namespace Purview.EventSourcing.Sqlite;

interface ISqliteEventStoreTests
{
	Task DeleteAsync_GivenAggregateExistsWithLargeEvent_PermanentlyDeletesAllData();

	Task DeleteAsync_GivenAggregateExists_PermanentlyDeletesAllData();

	Task DeleteAsync_GivenPreviouslySavedAggregate_MarksAsDeleted();

	Task GetAggregateIdsAsync_GivenNAggregatesInTheStore_CorrectlyReturnsTheirIds(int aggregateCount);

	Task GetAggregateIdsAsync_GivenNonDeletedAggregatesAndDeletedAggregatesInTheStoreAndRequestingAll_CorrectlyReturnsAllIds(int nonDeletedAggregateIdCount, int deletedAggregateIdCount);

	Task GetAggregateIdsAsync_GivenNonDeletedAggregatesAndDeletedAggregatesInTheStoreAndRequestingOnlyNonDeleted_CorrectlyReturnsNonDeletedIdsOnly(int nonDeletedAggregateIdCount, int deletedAggregateIdCount);

	Task GetAsync_GivenAggregateIsDeletedAndDeletedModeIsSetToThrow_ThrowsEventStoreAggregateDeletedException();

	Task GetAtAsync_GivenAnAggregateWithSavedEvents_RecreatesAggregateToPreviousVersion(int previousEventsToCreate);

	Task GetDeletedAsync_GivenDeletedAggregate_ReturnsAggregate();

	Task GetOrCreateAsync_GivenAggregateDoesNotExist_CreatesNewAggregate();

	Task IsDeletedAsync_GivenDeletedAggregates_ReturnsTrue();

	Task IsDeletedAsync_GivenNonDeletedAggregates_ReturnsFalse();

	Task RestoreAsync_GivenPreviouslySavedAndDeletedAggregate_MarksAsNotDeleted();

	Task SaveAsync_GivenAggregateWithDataAnnotationsAndInvalidProperties_NoChangesAreMadeAndNotSaved();

	Task SaveAsync_GivenAggregateWithNoChanges_DoesNotSave();

	Task SaveAsync_GivenNewAggregateWithChanges_SavesAggregate();

	Task SaveAsync_GivenAggregateWithComplexProperty_SavesEventWithComplexProperty();
}
