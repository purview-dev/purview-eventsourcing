using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Helpers;

static partial class PipelineHelpers
{
	static EventPropertyApplyMethodDetails? CreateEventPropertyApplyMethodDetails(FieldDeclarationSyntax field, string? fullNamespace, IGenerationLogger? logger, CancellationToken cancellationToken)
	{
		var eventPropertyAttribute = SharedHelpers.GetEventPropertyAttribute(field, logger, cancellationToken);
		if (eventPropertyAttribute == null)
		{
			// It's just not a field we need for generation.
			return null;
		}

		var eventType = eventPropertyAttribute.EventTypeName.Value;
		var methodName = $"Apply{eventPropertyAttribute.PropertyName.Value}";

		return new(
			MethodName: methodName,
			EventType: fullNamespace + eventType,
			null
		);
	}

	static ImmutableArray<EventPropertyApplyMethodDetails> GetGeneratedApplyMethods(ClassDeclarationSyntax classDeclaration, ImmutableArray<FoundPreDefinedApplyMethod> predefinedMethods, string? fullNamespace, IGenerationLogger? logger, CancellationToken cancellationToken)
	{
		var fields = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>().ToArray();
		List<EventPropertyApplyMethodDetails> generatedApplyMethods = [];
		foreach (var field in fields)
		{
			var applyMethodDetails = CreateEventPropertyApplyMethodDetails(field, fullNamespace, logger, cancellationToken);
			if (applyMethodDetails != null)
			{
				var predefinedApplyMethod = predefinedMethods.FirstOrDefault(m =>
				{
					if (m.EventType == applyMethodDetails.EventType)
					{
						return true;
					}

					if (m.PartialMatch && fullNamespace != null)
					{
						// If it's a partial match, then we need to test the EventType against
						// the name of the event without the namespace.
						var length = fullNamespace.Length;

						return m.EventType == applyMethodDetails.EventType.Substring(length);
					}

					return false;
				});

				if (predefinedApplyMethod != null)
				{
					if (predefinedApplyMethod.PartialMatch)
					{
						applyMethodDetails = new(predefinedApplyMethod.MethodName, applyMethodDetails.EventType, predefinedApplyMethod);
					}
					else
					{
						continue;
					}
				}

				generatedApplyMethods.Add(applyMethodDetails);
			}
		}

		return [.. generatedApplyMethods];
	}

	static ImmutableArray<FoundPreDefinedApplyMethod> GetPreDefinedApplyMethods(ClassDeclarationSyntax classDeclaration, IGenerationLogger? logger, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		var methods = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
		List<FoundPreDefinedApplyMethod> foundPreDefinedApplyMethods = [];
		foreach (var method in methods)
		{
			var parameters = method.ParameterList.Parameters;
			if (parameters.Count != 1)
			{
				// If it's not a single parameter it can't be used for
				// applying an event anyway.
				continue;
			}

			var parameter = parameters[0];
			if (parameter.Type is not IdentifierNameSyntax identifierNameSyntax)
			{
				continue;
			}

			var parameterTypeSymbol = semanticModel.GetDeclaredSymbol(parameter.Type, cancellationToken);
			if (parameterTypeSymbol == null)
			{
				logger?.Debug($"Unable to find type symbol for {parameter.Flatten()}, however this could be due to a generated event that does not have a symbol yet.");
			}

			var isPartialMatch = parameterTypeSymbol == null;
			var isEventType = false;
			var eventType = identifierNameSyntax.Identifier.Text;
			if (!isPartialMatch)
			{
				isEventType = parameterTypeSymbol!.ContainingType.AllInterfaces.Any(Constants.EventStore.IEvent.Equals)
				|| Constants.EventStore.EventBase.Equals(parameterTypeSymbol.ContainingType.BaseType);

				if (!isEventType)
				{
					continue;
				}

				eventType = Utilities.GetFullyQualifiedName(parameterTypeSymbol.ContainingType);
			}

			var isVoid = method.ReturnType is PredefinedTypeSyntax predefinedTypeSyntax && predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword);
			var isPublic = method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));

			foundPreDefinedApplyMethods.Add(new(
				isPartialMatch,
				method.Identifier.Text,
				eventType, // This needs to be the full type name with namespace/ parent classes.
				isVoid,
				isPublic,
				method.GetLocation()
			));
		}

		return [.. foundPreDefinedApplyMethods];
	}
}
