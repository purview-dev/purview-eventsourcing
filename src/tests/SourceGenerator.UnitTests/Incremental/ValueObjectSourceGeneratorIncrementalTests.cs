using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using StepReason = Microsoft.CodeAnalysis.IncrementalStepRunReason;

namespace Purview.EventSourcing.SourceGenerator.Incremental;

/// <summary>
/// Per-target incremental caching tests for the value-object pipeline, driven by the framework's
/// <c>GenerateIncrementalAsync</c> runner over a single shared <c>GeneratorDriver</c>.
/// </summary>
public sealed class ValueObjectSourceGeneratorIncrementalTests : ValueObjectSourceGeneratorTestBase
{
	const string EmailScalarSource = """
		using Purview.EventSourcing.Serialization;

		namespace Testing
		{
			[Scalar]
			public readonly partial record struct EmailAddress
			{
				public string Value { get; }
			}
		}
		""";

	const string ModifiedEmailScalarSource = """
		using Purview.EventSourcing.Serialization;

		namespace Testing
		{
			[Scalar(GenerateEmpty = false)]
			public readonly partial record struct EmailAddress
			{
				public string Value { get; }
			}
		}
		""";

	const string MoneyScalarSource = """
		using Purview.EventSourcing.Serialization;

		namespace Testing
		{
			[Scalar("Amount")]
			public readonly partial record struct Money
			{
				public decimal Amount { get; }
			}
		}
		""";

	[Test]
	public async Task Generate_FirstRun_AllScalarTargetsNew(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync(
			[new IncrementalRunInput([EmailScalarSource, MoneyScalarSource])],
			cancellationToken: cancellationToken
		);

		var reasons = StepReasons(result.Runs[0], "GetScalarValueObjectTargets");
		await Assert.That(reasons.Length).IsEqualTo(2);
		await Assert.That(reasons.All(static reason => reason == StepReason.New)).IsTrue();
	}

	[Test]
	public async Task Generate_RerunWithUnchangedCompilation_ScalarTargetsCached(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync(
			[EmailScalarSource, MoneyScalarSource],
			cancellationToken: cancellationToken
		);

		var reasons = StepReasons(result.Runs[1], "GetScalarValueObjectTargets");
		await Assert.That(reasons.Length).IsEqualTo(2);
		await Assert.That(reasons.All(static reason => reason is StepReason.Cached or StepReason.Unchanged)).IsTrue();
	}

	[Test]
	public async Task Generate_GivenChangeToOneScalar_OnlyThatTargetModified(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync(
			[
				new IncrementalRunInput([EmailScalarSource, MoneyScalarSource]),
				new IncrementalRunInput([ModifiedEmailScalarSource, MoneyScalarSource]),
			],
			cancellationToken: cancellationToken
		);

		var reasons = StepReasons(result.Runs[1], "GetScalarValueObjectTargets");
		await Assert.That(reasons.Length).IsEqualTo(2);
		await Assert.That(reasons.Count(static reason => reason == StepReason.Modified)).IsEqualTo(1);
		await Assert.That(reasons.Count(static reason => reason == StepReason.New)).IsEqualTo(0);
	}

	[Test]
	public async Task Generate_GivenScalarDeleted_TargetRemoved(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync(
			[
				new IncrementalRunInput([EmailScalarSource, MoneyScalarSource]),
				new IncrementalRunInput([EmailScalarSource]),
			],
			cancellationToken: cancellationToken
		);

		var reasons = StepReasons(result.Runs[1], "GetScalarValueObjectTargets");
		await Assert.That(reasons.Length).IsEqualTo(2);
		await Assert.That(reasons.Count(static reason => reason == StepReason.Removed)).IsEqualTo(1);
		await Assert.That(reasons.Count(static reason => reason == StepReason.Modified)).IsEqualTo(0);
	}

	static ImmutableArray<StepReason> StepReasons(IncrementalCacheRun run, string stepName) =>
		[
			.. run.Steps.TryGetValue(stepName, out var steps)
				? steps.SelectMany(static step => step.Outputs.Select(static output => output.Reason))
				: [],
		];
}
