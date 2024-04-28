using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Emitters;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingSourceGenerator
{
	static void RegisterIAggregate(IncrementalGeneratorInitializationContext context, IGenerationLogger? logger)
	{
		// Transform
		Func<GeneratorAttributeSyntaxContext, CancellationToken, IAggregateGenerationTarget?> iAggregateTransform =
			logger == null
				? static (context, cancellationToken) => PipelineHelpers.BuildIAggregateTransform(context, null, cancellationToken)
				: (context, cancellationToken) => PipelineHelpers.BuildIAggregateTransform(context, logger, cancellationToken);

		// Register
		var generateIAggregateTargets = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				Constants.Core.GenerateAggregateAttribute,
				static (node, token) => PipelineHelpers.HasIsIAggregateAttribute(node, token),
				iAggregateTransform
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(EventSourcingSourceGenerator)}_GenerateAggregates");

		// Build generation (static vs. non-static is or the logger).
		Action<SourceProductionContext, (Compilation Compilation, ImmutableArray<IAggregateGenerationTarget?> Targets)> generationIAggregateAction =
			logger == null
				? static (spc, source) => GenerateAggregateTargets(source.Targets, spc, null)
				: (spc, source) => GenerateAggregateTargets(source.Targets, spc, logger);

		// Register with the source generator.
		var iAggregateTargetsCombined = context.CompilationProvider.Combine(generateIAggregateTargets.Collect());
		context.RegisterImplementationSourceOutput(
			source: iAggregateTargetsCombined,
			action: generationIAggregateAction
		);
	}

	static void GenerateAggregateTargets(ImmutableArray<IAggregateGenerationTarget?> targets, SourceProductionContext spc, IGenerationLogger? logger)
	{
		if (targets.Length == 0)
		{
			return;
		}

		try
		{
			foreach (var target in targets)
			{
				logger?.Debug($"IAggregate generation target: {target!.FullyQualifiedName}");

				IAggregateImplementationEmitter.GenerateImplementation(target!, spc, logger);
			}
		}
		catch (Exception ex)
		{
			logger?.Error($"A fatal error occurred while executing the source generation stage: {ex}");

			EventSourcingDiagnostics.Report(spc.ReportDiagnostic, EventSourcingDiagnostics.General.FatalExecutionDuringExecution, null, ex);
		}
	}
}
