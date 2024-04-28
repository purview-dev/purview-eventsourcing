namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingSourceGeneratorTests
{
	[Fact]
	public async Task Generate_GivenGenerationIAggregateAndNoProperties_GeneratesIAggregate()
	{
		// Arrange
		const string basicAggregate = @"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

[GenerateAggregate]
public partial class TestAggregate : IAggregate {
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Theory]
	[InlineData("generateImplicitly: true")]
	[InlineData("GenerateImplicitly = true")]
	public async Task Generate_GivenGenerationIAggregateWithImplicitGenerationSetToTrue_GeneratesIAggregateImplicitly(string attribute)
	{
		// Arrange
		string basicAggregate = @$"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

[GenerateAggregate({attribute})]
public partial class TestAggregate : IAggregate {{
}}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Theory]
	[InlineData("generateImplicitly: false")]
	[InlineData("GenerateImplicitly = false")]
	public async Task Generate_GivenGenerationIAggregateWithImplicitGenerationSetToFalse_GeneratesIAggregateExplicitly(string attribute)
	{
		// Arrange
		string basicAggregate = @$"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

[GenerateAggregate({attribute})]
public partial class TestAggregate : IAggregate {{
}}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult);
	}
}
