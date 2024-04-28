using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

partial class EventTargetClassEmitter
{
	static int EmitNamespaceStart(EventPropertyGenerationTarget target, StringBuilder builder,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating namespace for: {target.FullyQualifiedName}");

		var indent = 0;
		if (target.AggregateClassNamespace != null)
		{
			builder
				.Append("namespace ")
				.AppendLine(target.AggregateClassNamespace)
			;

			builder
				.Append('{')
				.AppendLine();

			indent++;
		}

		if (target.AggregateParentClasses.Length > 0)
		{
			logger?.Debug($"Generating parent partial classes for: {target.FullyQualifiedName}");

			foreach (var parentClass in target.AggregateParentClasses.Reverse())
			{
				builder
					.Append(indent, "partial class ", withNewLine: false)
					.Append(parentClass)
					.AppendLine()
					.Append(indent, "{");

				indent++;
			}
		}

		return indent++;
	}

	static void EmitNamespaceEnd(EventPropertyGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating namespace for: {target.FullyQualifiedName}");

		if (target.AggregateParentClasses.Length > 0)
		{
			foreach (var parentClass in target.AggregateParentClasses)
			{
				builder
					.Append(--indent, '}')
				;
			}
		}

		if (target.AggregateClassNamespace != null)
		{
			builder
				.Append('}')
			;
		}
	}
}
