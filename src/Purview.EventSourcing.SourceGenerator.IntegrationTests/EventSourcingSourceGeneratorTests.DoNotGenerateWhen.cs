namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingSourceGeneratorTests
{
	[Fact]
	public async Task Generate_GivenApplyAlreadyExists_DoesNotGenerateApplyMethod()
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

	void ApplyStringValueNonGenerated(StringValueEvent @event) {
	}
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult);
	}
}
