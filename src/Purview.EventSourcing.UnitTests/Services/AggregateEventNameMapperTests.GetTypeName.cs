﻿namespace Purview.EventSourcing;

partial class AggregateEventNameMapperTests
{
	[Fact]
	public void GetTypeName_GivenEventTypeNameIsNotInCollection_ReturnsNull()
	{
		// Arrange
		var mapper = CreateMapper<CorrectlyNamedAggregate>();
		const string missingEventTypeName = "no-event-type";

		// Act
		var result = mapper.GetTypeName<CorrectlyNamedAggregate>(missingEventTypeName);

		// Assert
		result
			.Should()
			.BeNull();
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("    ")]
	[InlineData(null)]
	public void GetTypeName_GivenEventTypeNameIsNullOrWhitespace_ThrowsArgumentNullException(string? eventTypeName)
	{
		// Arrange
		var mapper = CreateMapper<CorrectlyNamedAggregate>();

		// Act
		var action = () => mapper.GetTypeName<CorrectlyNamedAggregate>(eventTypeName!);

		// Assert
		action
			.Should()
			.ThrowExactly<ArgumentNullException>()
			.And
			.ParamName
			.Should()
			.Be(nameof(eventTypeName));
	}
}
