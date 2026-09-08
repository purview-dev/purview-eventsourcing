using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.EventSourcing.SourceGenerator.Common;

/// <summary>
/// Builds the semantic event contract and the value-based location map for an aggregate.
/// The semantic contract derives entirely from <see cref="AggregateInfo"/> so it is deterministic
/// and trivia-invariant; locations are captured separately from symbols purely for diagnostics.
/// </summary>
static class EventContractBuilder
{
	public static AggregateContract Build(AggregateInfo info)
	{
		var events = info.Methods.Select(BuildEvent).ToImmutableArray();

		return new(
			info.AggregateClass.Identity.Name,
			info.AggregateClass.Identity.Namespace ?? string.Empty,
			new(events)
		);
	}

	public static AggregateContractLocations BuildLocations(
		AggregateInfo info,
		INamedTypeSymbol classSymbol,
		ClassDeclarationSyntax syntax
	)
	{
		var aggregateKey = EventContractManifestLibrary.CreateAggregateKey(
			info.AggregateClass.Identity.Namespace ?? string.Empty,
			info.AggregateClass.Identity.Name
		);
		var aggregateLocation = ContractLocation.FromLocation(syntax.Identifier.GetLocation());
		var entries = ImmutableArray.CreateBuilder<ContractLocationEntry>();

		foreach (var method in info.Methods)
		{
			var symbol = classSymbol.GetMembers(method.MethodName).OfType<IMethodSymbol>().FirstOrDefault();
			var location = ContractLocation.FromLocation(symbol?.Locations.FirstOrDefault());
			if (location is null)
				continue;

			entries.Add(
				new(
					EventContractManifestLibrary.CreateEventKey(
						info.AggregateClass.Identity.Namespace ?? string.Empty,
						info.AggregateClass.Identity.Name,
						method.MethodName
					),
					location.Value
				)
			);
		}

		return new(aggregateKey, aggregateLocation, new(entries.ToImmutable()));
	}

	static EventContractEntry BuildEvent(AggregateEventMethodInfo method) =>
		new(
			method.EventType.Identity.Name,
			method.EventType.Identity.Namespace ?? string.Empty,
			method.MethodName,
			method.Version,
			new(BuildFields(method))
		);

	static ImmutableArray<EventContractField> BuildFields(AggregateEventMethodInfo method)
	{
		var builder = ImmutableArray.CreateBuilder<EventContractField>(method.EventParameters.Count);
		foreach (var parameter in method.EventParameters)
		{
			builder.Add(
				new(
					parameter.PropertyName,
					BuildFieldTypeToken(parameter.PropertyType),
					GetElementType(parameter.PropertyType),
					parameter.PropertyType.IsArray,
					parameter.PropertyType.IsNullable,
					parameter.IsRequired,
					parameter.IsString
				)
			);
		}

		return builder
			.ToImmutable()
			.Sort(
				(left, right) =>
				{
					var nameComparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
					return nameComparison != 0 ? nameComparison : StringComparer.Ordinal.Compare(left.Type, right.Type);
				}
			);
	}

	static string BuildFieldTypeToken(TypeReference type)
	{
		if (type.IsArray)
		{
			var element = GetArrayElement(type);
			var elementToken = element.IsEmpty ? "unknown" : NormalizeTypeName(element.RenderFullName);
			return $"{elementToken}[]";
		}

		return NormalizeTypeName(type.RenderFullName);
	}

	/// <summary>
	/// Nullability is tracked by <see cref="EventContractField.IsNullable"/> rather than embedded in the
	/// type token, so a nullable-to-non-nullable change is reported as a requiredness regression rather
	/// than an opaque type change.
	/// </summary>
	static string NormalizeTypeName(string render) =>
		render.EndsWith("?", StringComparison.Ordinal) ? render.Substring(0, render.Length - 1) : render;

	static TypeReference GetArrayElement(TypeReference type) =>
		type.Identity.TypeArguments.Length > 0 ? type.Identity.TypeArguments[0] : TypeReference.Empty;

	static string? GetElementType(TypeReference type)
	{
		if (!type.IsArray)
			return null;

		var element = GetArrayElement(type);
		return element.IsEmpty ? null : NormalizeTypeName(element.RenderFullName);
	}
}
