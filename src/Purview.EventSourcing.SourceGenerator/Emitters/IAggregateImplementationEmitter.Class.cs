using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

partial class IAggregateImplementationEmitter
{
	static int EmitClassStart(IAggregateGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		builder
			.Append(indent, "partial class ", withNewLine: false)
			.AppendLine(target.ClassName)
			.Append(indent, '{')
		;

		return indent++;
	}

	static int EmitClassEnd(StringBuilder builder, int indent,
		SourceProductionContext context)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		builder
			.Append(indent, '}')
		;

		return indent--;
	}
}
