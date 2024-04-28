﻿using Purview.EventSourcing.Aggregates.Test;

namespace Purview.EventSourcing.Aggregates;

partial class AggregateBaseTests
{
	[Theory]
	[InlineData(10, 10)]
	[InlineData(10, 20)]
	[InlineData(10, 0)]
	[InlineData(10, 100)]
	[InlineData(0, 1)]
	[InlineData(0, 10)]
	[InlineData(0, 100)]
	public void ClearUnsavedEvents_GivenNoUpperBoundVersionIsSpecifiedAndHasEvents_ReturnsToPreUnSavedAndAppliedEventVerison(int savedEventCount, int unSavedEventCount)
	{
		// Arrange
		TestAggregate testAggregate = CreateTestAggregate();
		testAggregate.Details.SavedVersion = testAggregate.Details.CurrentVersion = savedEventCount;

		for (int i = 0; i < unSavedEventCount; i++)
		{
			testAggregate.Increment();
		}

		testAggregate
			.Details
			.CurrentVersion
			.Should()
			.Be(savedEventCount + unSavedEventCount);

		// Act
		testAggregate.ClearUnsavedEvents(upToVersion: null);

		// Assert
		testAggregate
			.Details
			.CurrentVersion
			.Should()
			.Be(savedEventCount);

		testAggregate
			.HasUnsavedEvents()
			.Should()
			.BeFalse();
	}

	[Theory]
	[InlineData(100, 10)]
	[InlineData(21, 20)]
	[InlineData(10, 1)]
	[InlineData(10, 9)]
	[InlineData(2, 1)]
	[InlineData(11, 10)]
	[InlineData(111, 100)]
	public void ClearUnsavedEvents_GivenUpperBoundVersionIsSpecifiedAndIsLessThanEventsUnsaved_ReturnsToPreUnSavedUpToSpecifiedBound(int unSavedEventCount, int eventsToRemove)
	{
		// Arrange
		TestAggregate testAggregate = CreateTestAggregate();

		for (int i = 0; i < unSavedEventCount; i++)
		{
			testAggregate.Increment();
		}

		// Act
		testAggregate.ClearUnsavedEvents(upToVersion: eventsToRemove);

		// Assert
		testAggregate
			.Details
			.CurrentVersion
			.Should()
			.Be(unSavedEventCount - eventsToRemove);
	}
}
