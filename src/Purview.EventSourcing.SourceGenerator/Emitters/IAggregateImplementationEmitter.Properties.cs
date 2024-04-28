using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

partial class IAggregateImplementationEmitter
{
	static int EmitProperties(IAggregateGenerationTarget target, StringBuilder builder, int indent,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (target.GenerateAggregateTypeProperty)
		{
			builder
				.Append(indent + 1, PublicOrImplicitModifier(target, Constants.Shared.String), withNewLine: false)
				.Append(' ')
				.Append(PublicOrImplicitIAggregate(target))
				.Append("AggregateType { get; } = ")
				.Append(target.AggregateTypeName!.Wrap())
				.AppendLine(";")
				.AppendLine();
			;
		}
		else
		{
			logger?.Debug($"Skipping AggregateType property gen: {target.FullyQualifiedName}.");
		}

		builder
			.Append(indent + 1, PublicOrImplicitModifier(target, Constants.EventStore.AggregateDetails), withNewLine: false)
			.Append(' ')
			.Append(PublicOrImplicitIAggregate(target))
			.Append("Details { get; init; } = new ")
			.Append(Constants.EventStore.AggregateDetails)
			.AppendLine("();")
			.AppendLine()
		;

		return indent;
	}
}
