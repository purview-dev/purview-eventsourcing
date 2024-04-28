using Purview.EventSourcing.Services;

namespace Purview.EventSourcing;

partial class AggregateEventNameMapperTests
{
	[Fact]
	public void GetName_GivenEventTypeEndingWithEvent_ReturnsMappedName()
	{
		// Arrange
		AggregateEventNameMapper mapper = CreateMapper<CorrectlyNamedAggregate>();
		Type eventType = typeof(EventTypeEndingInEvent);

		// Act
		string result = mapper.GetName<CorrectlyNamedAggregate>(eventType);

		// Assert
		result
			.Should()
			.Be($"{_correctlyNamedAggregateName}.event-type-ending-in");
	}

	[Fact]
	public void GetName_GivenEventTypeNotEndingInEvent_ReturnsTypeName()
	{
		// Arrange
		AggregateEventNameMapper mapper = CreateMapper<CorrectlyNamedAggregate>();
		Type eventType = typeof(EventTypeNotEndingInEvent2);

		// Act
		string result = mapper.GetName<CorrectlyNamedAggregate>(eventType);

		// Assert
		result
			.Should()
			.Be(typeof(EventTypeNotEndingInEvent2).FullName);
	}
}
