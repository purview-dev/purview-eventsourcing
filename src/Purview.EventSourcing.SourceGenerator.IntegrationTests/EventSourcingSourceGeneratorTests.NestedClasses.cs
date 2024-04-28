namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingSourceGeneratorTests
{

	[Fact]
	public async Task Generate_GivenAggregateIsNestedClass_GeneratesWithinNestedClass()
	{
		// Arrange
		string basicAggregate = @"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

public partial class ParentClass
{
	[GenerateAggregate]
	public partial class NestedTestAggregate : IAggregate
	{
		[EventProperty(EventTypeName = ""TheBadger___Event"")]
		string? _stringValue;
	}
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenAggregateIsNestedClasses_GeneratesWithinNestedClasses()
	{
		// Arrange
		string basicAggregate = @"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

public partial class ParentClass {
	public partial class ParentClass2 {
		[GenerateAggregate]
		public partial class TestAggregate : IAggregate {
			[EventProperty(EventTypeName = ""TheBadger_EVENT"")]
			string? _stringValue;
		}
	}
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult);
	}
}
