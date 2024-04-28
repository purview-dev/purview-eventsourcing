namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingSourceGeneratorTests
{
	[Fact]
	public async Task Generate_GivenAggregateDoesNotImplementIAggregate_GeneratesDiagnostic()
	{
		// Arrange
		const string basicAggregate = @"
using Purview.EventSourcing;

namespace Testing;

[GenerateAggregate]
public partial class TestAggregate
{
	[EventProperty]
	string? _stringValue;
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult,
			v => v.ScrubInlineGuids(),
			validateNonEmptyDiagnostics: true,
			validationCompilation: false
		);
	}

	[Fact]
	public async Task Generate_GivenAggregateClassIsNotPartial_GeneratesDiagnostic()
	{
		// Arrange
		const string basicAggregate = @"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

[GenerateAggregate]
public class TestAggregate : IAggregate
{
	[EventProperty]
	string? _stringValue;
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult,
			v => v.ScrubInlineGuids(),
			validateNonEmptyDiagnostics: true,
			validationCompilation: false
		);
	}

	[Fact]
	public async Task Generate_GivenAggregateDoesNotHaveGenerateAggregate_GeneratesDiagnostic()
	{
		// Arrange
		const string basicAggregate = @"
using Purview.EventSourcing;
using Purview.EventSourcing.Interfaces.Aggregates;

namespace Testing;

public partial class TestAggregate : IAggregate {
	[EventProperty]
	string? _stringValue;
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult,
			v => v.ScrubInlineGuids(),
			validateNonEmptyDiagnostics: true,
			validationCompilation: false
		);
	}

	[Fact]
	public async Task Generate_GivenApplyMethodIsPublic_GeneratesDiagnostic()
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

	public void Apply(StringValueEvent e) {
	}
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult, v => v.ScrubInlineGuids(), validateNonEmptyDiagnostics: true);
	}

	[Fact]
	public async Task Generate_GivenApplyMethodHasReturnValue_GeneratesDiagnostic()
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

	bool Apply(StringValueEvent e) {
		return true;
	}
}
";

		// Act
		GenerationResult generationResult = await GenerateAsync(basicAggregate);

		// Assert
		await TestHelpers.Verify(generationResult, v => v.ScrubInlineGuids(), validateNonEmptyDiagnostics: true);
	}
}
