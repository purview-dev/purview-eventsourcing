using Purview.EventSourcing.Aggregates.Exceptions;
using Purview.EventSourcing.Aggregates.Test;

namespace Purview.EventSourcing.Aggregates;

partial class AggregateBaseTests
{
	[Fact]
	public void Id_GivenIdAlreadySet_ThrowsArgumentIdAlreadySetException()
	{
		// Arrange
		TestAggregate aggregate = CreateTestAggregate("Aggregate-Id");

		// Act
		Action action = () => aggregate.Details.Id = "Another Id";

		// Assert
		action
			.Should()
			.ThrowExactly<IdAlreadySetException>();
	}

	[Fact]
	public void Id_GivenIdAlreadySetAndIdIsSetToTheSame_DoesNotThrowException()
	{
		// Arrange
		const string AggregateId = "Aggregate-Id";

		TestAggregate aggregate = CreateTestAggregate(AggregateId);

		// Act
		Action action = () => aggregate.Details.Id = AggregateId;

		// Assert
		action
			.Should()
			.NotThrow<IdAlreadySetException>();
	}
}
