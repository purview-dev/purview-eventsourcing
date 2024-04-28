using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

partial class EventTargetClassEmitter
{
	static int EmitEventClassStart(EventPropertyGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating event class: {target.FullyQualifiedName}");

		builder
			.Append(indent, "sealed public partial class ", withNewLine: false)
			.Append(target.EventTypeName)
			.Append(" : ")
			.Append(Constants.EventStore.IEvent)
			.AppendLine()
			.Append(indent, '{')
		;

		return indent;
	}

	static void EmitEventClassEnd(EventPropertyGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating event class end: {target.FullyQualifiedName}");

		builder
			.Append(indent, '}')
		;
	}

	static int EmitAggregateClassStart(EventPropertyGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating aggregate class: {target.FullyQualifiedName}");

		builder
			.Append(indent, "partial class ", withNewLine: false)
			.AppendLine(target.AggregateClassName)
			.Append(indent, '{')
		;

		return indent;
	}

	static void EmitAggregateClassEnd(EventPropertyGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating event class end: {target.FullyQualifiedName}");

		builder
			.Append(indent, '}')
		;
	}
}
