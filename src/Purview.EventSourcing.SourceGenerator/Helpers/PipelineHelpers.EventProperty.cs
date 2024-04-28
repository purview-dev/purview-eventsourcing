using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Helpers;

partial class PipelineHelpers
{

	public static bool HasEventPropertyAttribute(SyntaxNode _, CancellationToken __) => true;

	public static EventPropertyGenerationTarget? BuildEventPropertyTransform(GeneratorAttributeSyntaxContext context, IGenerationLogger? logger, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		if (context.TargetNode is not VariableDeclaratorSyntax variableDeclarator)
		{
			logger?.Error($"Could not find variable syntax from the target node '{context.TargetNode.Flatten()}'.");
			return null;
		}

		if (context.TargetSymbol is not IFieldSymbol fieldSymbol)
		{
			logger?.Error($"Could not find field symbol '{variableDeclarator.Flatten()}'.");
			return null;
		}

		var semanticModel = context.SemanticModel;
		var eventPropertyAttribute = SharedHelpers.GetEventPropertyAttribute(fieldSymbol.Name, context.Attributes[0], semanticModel, logger, token);
		if (eventPropertyAttribute == null)
		{
			return null;
		}

		var parentClass = Utilities.GetParentClass(variableDeclarator);
		if (parentClass == null)
		{
			logger?.Error($"Could not find parent class in declared field '{variableDeclarator.Flatten()}'.");

			return null;
		}

		token.ThrowIfCancellationRequested();

		var classContainsGenerateAggregate = parentClass.AttributeLists
			.SelectMany(m => m.Attributes)
			.Any(Constants.Core.GenerateAggregateAttribute.Equals);
		var fullNamespace = Utilities.GetFullNamespace(parentClass, true);
		var propertyType = Utilities.GetFullyQualifiedName(fieldSymbol.Type);
		var predefinedApplyMethods = GetPreDefinedApplyMethods(parentClass, logger, semanticModel, token);
		var generatedApplyMethod = predefinedApplyMethods.FirstOrDefault(m =>
		{
			if (m.EventType == eventPropertyAttribute.EventTypeName.Value)
			{
				return true;
			}

			if (m.PartialMatch && fullNamespace != null && eventPropertyAttribute.EventTypeName.Value != null)
			{
				var len = fullNamespace.Length;
				if (eventPropertyAttribute.EventTypeName.Value.Length > len)
				{
					return m.EventType == eventPropertyAttribute.EventTypeName.Value.Substring(len);
				}
			}

			return false;
		});
		var generateApplyMethod = generatedApplyMethod == null;
		var applyMethodName = generateApplyMethod
			? $"Apply{eventPropertyAttribute.PropertyName.Value}" // This is dup'd ):
			: generatedApplyMethod!.MethodName;

		token.ThrowIfCancellationRequested();

		return new(
			ParentClassHasGenerateAggregate: classContainsGenerateAggregate,
			AggregateClassName: parentClass.Identifier.Text,
			AggregateClassNamespace: Utilities.GetNamespace(parentClass),
			AggregateParentClasses: Utilities.GetParentClasses(parentClass),
			FullyQualifiedName: fullNamespace + eventPropertyAttribute.EventTypeName.Value,
			EventTypeName: eventPropertyAttribute.EventTypeName.Value!,
			FieldName: fieldSymbol.Name,
			PropertyName: eventPropertyAttribute.PropertyName.Value!,
			PropertyType: propertyType,
			IsNullable: fieldSymbol.Type.NullableAnnotation == NullableAnnotation.Annotated,
			PrivateSetter: eventPropertyAttribute.PrivateSetter.Value!.Value,
			GenerateApplyMethod: generateApplyMethod,
			ApplyMethodName: applyMethodName,
			FieldLocation: variableDeclarator.GetLocation()
		);
	}
}
