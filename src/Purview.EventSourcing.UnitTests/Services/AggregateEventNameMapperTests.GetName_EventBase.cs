using Purview.EventSourcing.Services;

namespace Purview.EventSourcing;

partial class AggregateEventNameMapperTests
{
	[Fact]
	public void GetName_GivenEventInstanceWithDefinedName_ReturnsFullTypeName()
	{
		// Arrange
		AggregateEventNameMapper mapper = CreateMapper<CorrectlyNamedAggregate>();
		EventTypeEndingInEvent @event = new();

		// Act
		string result = mapper.GetName<CorrectlyNamedAggregate>(@event);

		// Assert
		result
			.Should()
			.Be($"{_correctlyNamedAggregateName}.event-type-ending-in");
	}

	[Fact]
	public void GetName_GivenEventInstanceWithNoDefinedName_ReturnsFullTypeName()
	{
		// Arrange
		AggregateEventNameMapper mapper = CreateMapper<CorrectlyNamedAggregate>();
		EventTypeNotEndingInEvent2 @event = new();

		// Act
		string result = mapper.GetName<CorrectlyNamedAggregate>(@event);

		// Assert
		result
			.Should()
			.Be(typeof(EventTypeNotEndingInEvent2).FullName);
	}

	[Theory]
	[InlineData("Purview.Services.UserProfile.Aggregates.UserProfile.Events.ClearProfileAttributesEvent, Purview.EventSourcing.UnitTests", "clear-profile-attributes")]
	[InlineData("Purview.Services.UserProfile.Aggregates.UserProfile.Events.ClearRolesEvent, Purview.EventSourcing.UnitTests", "clear-roles")]
	public void GetName_GivenEventName_MatchesExpectation(string eventType, string expectation)
	{
		// Arrange
		AggregateEventNameMapper mapper = CreateMapper<CorrectlyNamedAggregate>();

		// Act
		Type? aggregateEventType = Type.GetType(eventType, true);
		aggregateEventType
			.Should()
			.NotBeNull();

		string result = mapper.GetName<CorrectlyNamedAggregate>(aggregateEventType!);

		// Assert
		result
			.Should()
			.Be($"{_correctlyNamedAggregateName}.{expectation}");
	}
}
