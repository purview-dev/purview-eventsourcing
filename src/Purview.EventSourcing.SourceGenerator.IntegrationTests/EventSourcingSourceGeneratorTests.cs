using Xunit.Abstractions;

namespace Purview.EventSourcing.SourceGenerator;

public partial class EventSourcingSourceGeneratorTests(ITestOutputHelper testOutputHelper) : IncrementalSourceGeneratorTestBase<EventSourcingSourceGenerator>(testOutputHelper)
{
	[Fact]
	public async Task Generate_GivenGeneratedAttributes_GeneratesAsExpected()
	{
		// Arrange
		const string empty = @"
using Purview.EventSourcing;

namespace Testing;

";

		// Act
		GenerationResult generationResult = await GenerateAsync(empty);

		// Assert
		await TestHelpers.Verify(generationResult, autoVerifyTemplates: false);
	}
}
