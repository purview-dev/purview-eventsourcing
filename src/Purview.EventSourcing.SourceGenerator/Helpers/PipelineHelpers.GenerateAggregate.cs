using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Helpers;

partial class PipelineHelpers
{
	public static bool HasIsIAggregateAttribute(SyntaxNode _, CancellationToken __) => true;

	public static IAggregateGenerationTarget? BuildIAggregateTransform(GeneratorAttributeSyntaxContext context, IGenerationLogger? logger, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		if (context.TargetNode is not ClassDeclarationSyntax classDeclaration)
		{
			logger?.Error($"Could not find class syntax from the target node '{context.TargetNode.Flatten()}'.");
			return null;
		}

		if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
		{
			logger?.Error($"Could not find class symbol '{classDeclaration.Flatten()}'.");
			return null;
		}

		var semanticModel = context.SemanticModel;
		var generateIAggregateAttribute = SharedHelpers.GetGenerateIAggregateAttribute(context.Attributes[0], semanticModel, logger, token);
		if (generateIAggregateAttribute == null)
		{
			logger?.Error($"Could not find {Constants.Core.GenerateAggregateAttribute} when one was expected '{classDeclaration.Flatten()}'.");

			return null;
		}

		var isPartial = classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
		var implementsIAggregate = classSymbol.Interfaces.Any(Constants.EventStore.IAggregate.Equals);

		token.ThrowIfCancellationRequested();

		var aggregateTypeProperty = classDeclaration
			.DescendantNodes()
			.OfType<PropertyDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.Text == "AggregateType");

		string? aggregateTypeName = null;
		if (aggregateTypeProperty == null)
		{
			aggregateTypeName = Utilities.GetAggregateName(classDeclaration.Identifier.Text);
		}

		token.ThrowIfCancellationRequested();

		var fullNamespace = Utilities.GetFullNamespace(classDeclaration, true);
		var foundPreDefinedApplyMethods = GetPreDefinedApplyMethods(classDeclaration, logger, semanticModel, token);
		var generatedApplyMethods = GetGeneratedApplyMethods(classDeclaration, foundPreDefinedApplyMethods, fullNamespace, logger, token);

		return new(
			ClassName: classSymbol.Name,
			ClassNamespace: Utilities.GetNamespace(classDeclaration),
			ParentClasses: Utilities.GetParentClasses(classDeclaration),
			FullNamespace: fullNamespace,
			FullyQualifiedName: fullNamespace + classSymbol.Name,
			PredefinedApplyMethods: foundPreDefinedApplyMethods.Where(m => !m.PartialMatch).ToImmutableArray(),
			GeneratedApplyMethods: generatedApplyMethods,
			IsPartialClass: isPartial,
			IAggregateInterfaceIsPresent: implementsIAggregate,
			GenerateIAggregateImplicitly: generateIAggregateAttribute.GenerateImplicitly.Value!.Value,
			GenerateAggregateTypeProperty: aggregateTypeProperty == null,
			AggregateTypeName: aggregateTypeName,
			ClassLocation: classDeclaration.GetLocation()
		);
	}
}
