using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.EventSourcing.SourceGenerator.Records;

namespace Purview.EventSourcing.SourceGenerator.Helpers;

static class SharedHelpers
{
	public static GenerateIAggregateAttributeRecord? GetGenerateIAggregateAttribute(
		AttributeData attributeData,
		SemanticModel semanticModel,
		IGenerationLogger? logger,
		CancellationToken token)
	{

		AttributeValue<bool>? generateImplicitly = null;

		if (!AttributeParser(attributeData,
		(name, value) =>
		{
			if (name.Equals("GenerateImplicitly", StringComparison.OrdinalIgnoreCase))
			{
				generateImplicitly = new((bool)value);
			}
		}, semanticModel, logger, token))
		{
			// Failed to parse correctly.
			return null;
		}

		return new(
			GenerateImplicitly: generateImplicitly ?? new(true)
		);
	}

	public static EventPropertyAttributeRecord? GetEventPropertyAttribute(
		string fieldName,
		AttributeData attributeData,
		SemanticModel semanticModel,
		IGenerationLogger? logger,
		CancellationToken token)
	{

		AttributeValue<bool>? privateSetter = null;
		AttributeStringValue? propertyName = null;
		AttributeStringValue? eventTypeName = null;

		var propertyNameFromField = GeneratePropertyName(fieldName);
		if (!AttributeParser(attributeData,
		(name, value) =>
		{
			if (name.Equals("PropertyName", StringComparison.OrdinalIgnoreCase))
			{
				propertyName = new((string)value)!;
			}
			else if (name.Equals("EventTypeName", StringComparison.OrdinalIgnoreCase))
			{
				eventTypeName = new((string)value)!;
			}
			else if (name.Equals("PrivateSetter", StringComparison.OrdinalIgnoreCase))
			{
				privateSetter = new((bool)value);
			}
		}, semanticModel, logger, token))
		{
			// Failed to parse correctly.
		}

		return new(
			PrivateSetter: privateSetter ?? new(true),
			PropertyName: propertyName ?? new(propertyNameFromField),
			EventTypeName: eventTypeName ?? new((propertyName?.Value ?? propertyNameFromField) + "Event")
		);
	}

	public static EventPropertyAttributeRecord? GetEventPropertyAttribute(
		FieldDeclarationSyntax field,
		IGenerationLogger? logger,
		CancellationToken token)
	{

		foreach (var attributeSyntax in field.AttributeLists.SelectMany(m => m.Attributes))
		{
			token.ThrowIfCancellationRequested();

			if (Constants.Core.EventPropertyAttribute.Equals(attributeSyntax))
			{
				AttributeValue<bool>? privateSetter = null;
				AttributeStringValue? propertyName = null;
				AttributeStringValue? eventTypeName = null;

				var propertyNameFromField = GeneratePropertyName(field.Declaration.Variables.First().Identifier.Text);
				if (!AttributeParser(attributeSyntax,
				(name, value, order) =>
				{
					if (name?.Equals("PropertyName", StringComparison.OrdinalIgnoreCase) == true || (name == null && order == 0))
					{
						propertyName = new(value.Trim('"'));
					}
					else if (name?.Equals("PrivateSetter", StringComparison.OrdinalIgnoreCase) == true || (name == null && order == 1))
					{
						if (bool.TryParse(value, out var privateSettingValue))
						{
							privateSetter = new(privateSettingValue);
						}
					}
					else if (name?.Equals("EventTypeName", StringComparison.OrdinalIgnoreCase) == true || (name == null && order == 2))
					{
						eventTypeName = new(value.Trim('"'))!;
					}
				}, logger, token))
				{
					// Failed to parse correctly.
				}

				return new(
					PrivateSetter: privateSetter ?? new(true),
					PropertyName: propertyName ?? new(propertyNameFromField),
					EventTypeName: eventTypeName ?? new((propertyName?.Value ?? propertyNameFromField) + "Event")
				);
			}
			else
			{
				logger?.Debug($"Found an unknown attribute: {attributeSyntax}.");
			}
		}

		return null;
	}

	static string GeneratePropertyName(string name)
	{
		if (name.Length < 2)
		{
			return name;
		}

		if (name.StartsWith("m_", StringComparison.InvariantCulture))
		{
			name = name.Substring(2);
		}
		else if (name[0] == '_')
		{
			name = name.Substring(1);
		}

		if (char.IsLower(name[0]))
		{
			name = char.ToUpperInvariant(name[0]) + name.Substring(1);
		}

		return name;
	}

	//static public bool AttributeParser(
	//	AttributeData attributeData,
	//	SemanticModel? semanticModel,
	//	IGenerationLogger? logger,
	//	CancellationToken cancellationToken)
	//	=> AttributeParser(attributeData, (_, __) => { }, semanticModel, logger, cancellationToken);

	public static bool AttributeParser(
		AttributeData attributeData,
		Action<string, object> namedArguments,
		SemanticModel? semanticModel,
		IGenerationLogger? logger,
		CancellationToken cancellationToken)
	{
		logger?.Debug($"Found attribute: {attributeData}");

		if (semanticModel != null && HasErrors(attributeData, semanticModel, logger, cancellationToken))
		{
			logger?.Warning($"Attribute has error: {attributeData}");
			return false;
		}

		var constructorMethod = attributeData.AttributeConstructor;
		if (constructorMethod == null)
		{
			logger?.Warning("Could not locate the attribute's constructor.");

			return false;
		}

		if (attributeData.ConstructorArguments.Any(t => t.Kind == TypedConstantKind.Error))
		{
			logger?.Warning("Constructor arguments have an error.");

			return false;
		}

		if (attributeData.NamedArguments.Any(t => t.Value.Kind == TypedConstantKind.Error))
		{
			logger?.Warning("Named arguments have an error.");

			return false;
		}

		// supports: [DefaultLogLevel(LogLevel.Information)]
		// supports: [DefaultLogLevel(level: LogLevel.Information)]
		var items = attributeData.ConstructorArguments;
		if (items.Length > 0)
		{
			for (var i = 0; i < items.Length; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (items[i].IsNull)
				{
					continue;
				}

				var name = constructorMethod.Parameters[i].Name;
				var value = Utilities.GetTypedConstantValue(items[i])!;
				if (Constants.Shared.String.Equals(constructorMethod.Parameters[i].Type))
				{
					var v = (string)value;
					if (string.IsNullOrWhiteSpace(v))
					{
						continue;
					}
				}

				namedArguments(name, value);
			}
		}

		// argument syntax takes parameters. e.g. Level = LogLevel.Information
		// supports: e.g. [DefaultLogLevel(Level = LogLevel.Information )]
		if (attributeData.NamedArguments.Any())
		{
			foreach (var namedArgument in attributeData.NamedArguments)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var value = Utilities.GetTypedConstantValue(namedArgument.Value)!;
				if (namedArgument.Value.Type == null)
				{
					logger?.Error($"Named argument {namedArgument.Key}'s type could not be determined.");
					continue;
				}

				if (Constants.Shared.String.Equals(namedArgument.Value.Type))
				{
					var v = (string)value;
					if (string.IsNullOrWhiteSpace(v))
					{
						continue;
					}
				}

				namedArguments(namedArgument.Key, value!);
			}
		}

		return true;
	}

	public static bool AttributeParser(
		AttributeSyntax attributeSyntax,
		Action<string?, string, int> namedArguments,
		IGenerationLogger? logger,
		CancellationToken cancellationToken)
	{
		logger?.Debug($"Found attribute (syntax): {attributeSyntax}");

		var arguments = attributeSyntax.ArgumentList?.Arguments;
		if (arguments != null)
		{
			var index = 0;
			foreach (var argument in arguments)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var name = argument.NameEquals?.Name.ToString() ?? argument.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault()?.ToString();
				var value = argument.Expression.ToString();

				if (value == null)
				{
					continue;
				}

				namedArguments(name, value, index);

				index++;
			}
		}

		return true;
	}

	static bool HasErrors(AttributeData attributeData,
		SemanticModel semanticModel,
		IGenerationLogger? logger,
		CancellationToken cancellationToken)
	{
		if (attributeData.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is not AttributeSyntax attributeSyntax)
		{
			return false;
		}

		var diagnostics = semanticModel.GetDiagnostics(attributeSyntax.Span, cancellationToken);
		if (diagnostics.Length > 0 && logger != null)
		{
			var d = diagnostics.Select(m => m.GetMessage(CultureInfo.InvariantCulture));
			logger.Debug("Attribute has diagnostics: \n" + string.Join("\n - ", d));
		}

		return diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
	}
}
