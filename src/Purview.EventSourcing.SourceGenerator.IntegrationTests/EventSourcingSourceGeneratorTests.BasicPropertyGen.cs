namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingSourceGeneratorTests
{
	[Fact]
	public async Task Generate_GivenBasicProperty_GeneratesPropertyEventAndApplier()
	{
		// Arrange
		const string basicAggregate = @"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

[GenerateAggregate]
public partial class TestAggregate : IAggregate {
	[EventProperty]
	string? _stringValue;
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Theory]
	[InlineData("(privateSetter: true)")]
	[InlineData("(privateSetter: false)")]
	[InlineData("(PrivateSetter = true)")]
	[InlineData("(PrivateSetter = false)")]
	public async Task Generate_GivenBasicPropertyWithExplicitPrivateSetter_GeneratesPropertyEventAndApplier(string privateSetter)
	{
		// Arrange
		string basicAggregate = @$"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

[GenerateAggregate]
public partial class TestAggregate : IAggregate {{
	[EventProperty{privateSetter}]
	string? _stringValue;
}}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult, v => v.UseParameters(privateSetter));
	}

	[Theory]
	[InlineData("(\"Badger\")")]
	[InlineData("(propertyName: \"Badger\")")]
	[InlineData("(PropertyName = \"Badger\")")]
	public async Task Generate_GivenBasicPropertyWithExplicitName_GeneratesPropertyEventAndApplier(string name)
	{
		// Arrange
		string basicAggregate = @$"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

[GenerateAggregate]
public partial class TestAggregate : IAggregate {{
	[EventProperty{name}]
	string? _stringValue;
}}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult, v => v.UseParameters(name));
	}
}
