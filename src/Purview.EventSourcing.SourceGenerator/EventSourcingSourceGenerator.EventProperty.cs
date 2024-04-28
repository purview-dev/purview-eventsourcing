using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Emitters;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator;

partial class EventSourcingSourceGenerator
{
	static void RegisterEventProperty(IncrementalGeneratorInitializationContext context, IGenerationLogger? logger)
	{
		// Transform
		Func<GeneratorAttributeSyntaxContext, CancellationToken, EventPropertyGenerationTarget?> eventPropertyTransform =
			logger == null
				? static (context, cancellationToken) => PipelineHelpers.BuildEventPropertyTransform(context, null, cancellationToken)
				: (context, cancellationToken) => PipelineHelpers.BuildEventPropertyTransform(context, logger, cancellationToken);

		// Register
		var generationEventPropertyTargets = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				Constants.Core.EventPropertyAttribute,
				static (node, token) => PipelineHelpers.HasEventPropertyAttribute(node, token),
				eventPropertyTransform
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(EventSourcingSourceGenerator)}_EventProperties");

		// Build generation (static vs. non-static is or the logger).
		Action<SourceProductionContext, (Compilation Compilation, ImmutableArray<EventPropertyGenerationTarget?> Targets)> generationEventPropertyAction =
			logger == null
				? static (spc, source) => GenerateEventPropertyTargets(source.Targets, spc, null)
				: (spc, source) => GenerateEventPropertyTargets(source.Targets, spc, logger);

		// Register with the source generator.
		var eventPropertyTargetsCombined = context.CompilationProvider.Combine(generationEventPropertyTargets.Collect());
		context.RegisterImplementationSourceOutput(
			source: eventPropertyTargetsCombined,
			action: generationEventPropertyAction
		);
	}

	static void GenerateEventPropertyTargets(ImmutableArray<EventPropertyGenerationTarget?> targets, SourceProductionContext spc, IGenerationLogger? logger)
	{
		if (targets.Length == 0)
		{
			return;
		}

		try
		{
			foreach (var target in targets)
			{
				logger?.Debug($"Event generation target: {target!.FullyQualifiedName}");

				EventTargetClassEmitter.GenerateImplementation(target!, spc, logger);
			}
		}
		catch (Exception ex)
		{
			logger?.Error($"A fatal error occurred while executing the source generation stage: {ex}");

			EventSourcingDiagnostics.Report(spc.ReportDiagnostic, EventSourcingDiagnostics.General.FatalExecutionDuringExecution, null, ex);
		}
	}
}
