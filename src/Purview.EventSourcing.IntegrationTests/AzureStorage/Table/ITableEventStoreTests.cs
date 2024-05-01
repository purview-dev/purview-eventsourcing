﻿
namespace Purview.EventSourcing.AzureStorage.Table;

public interface ITableEventStoreTests
{
	Task DeleteAsync_GivenAggregateExistsWithLargeEvent_PermanentlyDeletesAllData();

	Task DeleteAsync_GivenAggregateExists_PermanentlyDeletesAllData();

	Task DeleteAsync_GivenDelete_NotifiesChangeFeed();

	Task DeleteAsync_GivenPreviouslySavedAggregate_MarksAsDeleted();

	Task DeleteAsync_WhenTableStoreConfigRemoveDeletedFromCacheIsTrueAndPreviouslySavedAggregate_RemovesFromCache();

	Task GetAggregateIdsAsync_GivenNAggregatesInTheStore_CorrectlyReturnsTheirIds(int aggregateCount);

	Task GetAggregateIdsAsync_GivenNonDeletedAggregatesAndDeletedAggregatesInTheStoreAndRequestingAll_CorrectlyReturnsAllIds(int nonDeletedAggregateIdCount, int deletedAggregateIdCount);

	Task GetAggregateIdsAsync_GivenNonDeletedAggregatesAndDeletedAggregatesInTheStoreAndRequestingOnlyNonDeleted_CorrectlyReturnsNonDeletedIdsOnly(int nonDeletedAggregateIdCount, int deletedAggregateIdCount);

	Task GetAsync_GivenAggregateIsDeletedAndDeletedModeIsSetToThrow_ThrowsEventStoreAggregateDeletedException();

	Task GetAsync_GivenAnAggregateWithMoreEventsThanTheSnapshot_RecreatesAggregate(int eventsToCreate);

	Task GetAsync_GivenAnAggregateWithNonRegisteredEventType_RecreatesAggregateAndLogsCannotApplyEvent(int eventsToCreate, int numberOfOldEventsToCreate);

	Task GetAsync_GivenAnAggregateWithSavedEventsButNoSnapshot_RecreatesAggregate(int eventsToCreate);

	Task GetAsync_GivenAnAggregateWithUnknownEventType_RecreatesAggregateAndLogsUnknown(int eventsToCreate, int numberOfOldEventsToCreate);

	Task GetAtAsync_GivenAnAggregateWithSavedEvents_RecreatesAggregateToPreviousVersion(int previousEventsToCreate);

	Task GetDeletedAsync_GivenDeletedAggregate_ReturnsAggregate();

	Task GetEventRangeAsync_GivenARequestedRangeOfEvents_EventsAreReturnsInCorrectOrder(int eventsToCreate, int startEvent, int? endEvent);

	Task GetEventRangeAsync_GivenARequestedRangeOfEvents_GetsEventsRequested(int eventsToCreate, int startEvent, int? endEvent, int expectedEventCount);

	Task GetOrCreateAsync_GivenAggregateDoesNotExist_CreatesNewAggregate();

	Task IsDeletedAsync_GivenDeletedAggregates_ReturnsTrue();

	Task IsDeletedAsync_GivenNonDeletedAggregates_ReturnsFalse();

	Task RestoreAsync_GivenPreviouslySavedAndDeletedAggregate_MarksAsNotDeleted();

	Task SaveAsync_GivenAggregateWithChanges_NotifiesChangeFeed(int eventsToCreate);

	Task SaveAsync_GivenAggregateWithDataAnnotationsAndInvalidProperties_NoChangesAreMadeAndNotSaved();

	Task SaveAsync_GivenAggregateWithNoChanges_DoesNotNotifyChangeFeed();

	Task SaveAsync_GivenAggregateWithNoChanges_DoesNotSave();

	Task SaveAsync_GivenNewAggregateWithChanges_SavesAggregate();

	Task SaveAsync_GivenNewAggregateWithLargeChangesAndNoSnapshot_ReadsAggregateFromEvents();

	Task SaveAsync_GivenNewAggregateWithLargeChanges_SavesAggregateWithLargeEventRecord();

	Task SaveAsync_GivenStreamVersionWithoutVersionSetWhenSaved_StreamVersionHasCorrectEvent(int eventsToGenerate);

	Task SaveAsync_GivenAggregateWithComplexProperty_SavesEventWithComplexProperty();

	Task SaveAsync_GivenEventCountIsGreaterThanMaximumNumberOfAllowedInBatchOperation_BatchesEvents(int eventsToGenerate);

	Task SaveAsync_GivenEventCountIsGreaterThanMaximumNumberOfAllowedEventsInSaveOperation_ThrowsException(int eventsToGenerate);
}