using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

partial class IAggregateImplementationEmitter
{
	static int EmitNamespaceStart(IAggregateGenerationTarget target, StringBuilder builder,
		SourceProductionContext context)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var indent = 0;
		if (target.ClassNamespace != null)
		{
			builder
				.Append("namespace ")
				.AppendLine(target.ClassNamespace)
			;

			builder
				.Append('{')
				.AppendLine();

			indent++;
		}

		if (target.ParentClasses.Length > 0)
		{
			foreach (var parentClass in target.ParentClasses.Reverse())
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

	static void EmitNamespaceEnd(IAggregateGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (target.ParentClasses.Length > 0)
		{
			foreach (var parentClass in target.ParentClasses)
			{
				builder
					.Append(--indent, '}')
				;
			}
		}

		if (target.ClassNamespace != null)
		{
			builder
				.AppendLine('}')
			;
		}
	}
}
