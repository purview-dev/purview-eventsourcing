using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;
using Purview.EventSourcing.SourceGenerator.Templates;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

static partial class EventTargetClassEmitter
{
	public static void GenerateImplementation(EventPropertyGenerationTarget target,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		logger?.Debug($"Generating event for: {target.FullyQualifiedName}.{target.PropertyName}.");

		if (!target.ParentClassHasGenerateAggregate)
		{
			EventSourcingDiagnostics.Report(context.ReportDiagnostic, EventSourcingDiagnostics.Property.ParentDoesNotHaveGenerateAggregateAttribute, target.FieldLocation);
		}

		GenerateEventClass(target, context, logger);
		GenerateAggregateWithProperty(target, context, logger);
	}

	static void GenerateEventClass(EventPropertyGenerationTarget target,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		StringBuilder builder = new();

		var indent = EmitNamespaceStart(target, builder, context, logger);

		indent = EmitEventClassStart(target, builder, indent, context, logger);
		indent = EmitEventProperty(target, builder, indent, context, logger);
		indent = EmitEventMethods(target, builder, indent, context, logger);

		EmitEventClassEnd(target, builder, indent, context, logger);
		EmitNamespaceEnd(target, builder, indent, context, logger);

		var sourceText = EmbeddedResources.Instance.AddHeader(builder.ToString());
		var hintName = $"{target.FullyQualifiedName}.g.cs";

		context.AddSource(hintName, Microsoft.CodeAnalysis.Text.SourceText.From(sourceText, Encoding.UTF8));
	}

	static void GenerateAggregateWithProperty(EventPropertyGenerationTarget target,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		StringBuilder builder = new();

		if (target.IsNullable)
		{
			builder
				.AppendLine("#nullable enable")
				.AppendLine()
			;
		}

		var indent = EmitNamespaceStart(target, builder, context, logger);

		indent = EmitAggregateClassStart(target, builder, indent, context, logger);
		indent = EmitAggregateProperty(target, builder, indent, context, logger);
		indent = EmitAggregateMethods(target, builder, indent, context, logger);

		EmitAggregateClassEnd(target, builder, indent, context, logger);
		EmitNamespaceEnd(target, builder, indent, context, logger);

		var sourceText = EmbeddedResources.Instance.AddHeader(builder.ToString());
		var hintName = $"{target.FullyQualifiedName}.{target.PropertyName}.g.cs";

		context.AddSource(hintName, Microsoft.CodeAnalysis.Text.SourceText.From(sourceText, Encoding.UTF8));
	}
}
