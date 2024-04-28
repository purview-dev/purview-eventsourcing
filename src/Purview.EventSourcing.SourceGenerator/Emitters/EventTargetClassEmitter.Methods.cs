using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

partial class EventTargetClassEmitter
{
	static int EmitEventMethods(EventPropertyGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating methods for event: {target.FullyQualifiedName}");

		builder
			.AppendLine()
			.Append(indent + 1, "override public int GetHashCode()")
			.Append(indent + 1, '{')
			.Append(indent + 2, "return ", withNewLine: false)
			.Append(Constants.Shared.HashCode)
			.Append(".Combine(")
			.Append(target.PropertyName)
			.AppendLine(");")
			.Append(indent + 1, '}')
		;

		return indent;
	}

	static int EmitAggregateMethods(EventPropertyGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (target.GenerateApplyMethod)
		{
			logger?.Debug($"Generating apply method for: {target.PropertyName} on {target.FullyQualifiedName}");

			builder
				.AppendLine()
				.Append(indent + 1, "void ", withNewLine: false)
				.Append(target.ApplyMethodName)
				.Append('(')
				.Append(target.FullyQualifiedName)
				.AppendLine(" @event)")
				.Append(indent + 1, '{')
				.Append(indent + 2, target.PropertyName, withNewLine: false)
				.Append(" = @event.")
				.Append(target.PropertyName)
				.AppendLine(';')
				.Append(2, '}')
			;
		}
		else
		{
			logger?.Debug($"Skipping generation for apply method for: {target.PropertyName} on {target.FullyQualifiedName}");
		}

		return indent;
	}
}
