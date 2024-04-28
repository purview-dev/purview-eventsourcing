namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingSourceGeneratorTests
{
	[Theory]
	[InlineData("(propertyName: \"TheBadger\")")]
	[InlineData("(PropertyName = \"TheBadger\")")]
	[InlineData("(\"TheBadger\")")]
	[InlineData("(\"TheBadger\", true)")]
	public async Task Generate_GivenBasicPropertyWithSpecifiedPropertyName_GeneratesPropertyEventAndApplier(string propertyName)
	{
		// Arrange
		string basicAggregate = @$"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

[GenerateAggregate]
public partial class TestAggregate : IAggregate {{
	[EventProperty{propertyName}]
	string? _stringValue;
}}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult, v => v.UseParameters(propertyName));
	}

	[Fact]
	public async Task Generate_GivenBasicPropertyWithSpecifiedEventTypeName_GeneratesEvent()
	{
		// Arrange
		string basicAggregate = @"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

[GenerateAggregate]
public partial class TestAggregate : IAggregate {
	[EventProperty(EventTypeName = ""TheBadger"")]
	string? _stringValue;
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult);
	}
}
