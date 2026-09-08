using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.EventSourcing.SourceGenerator.Aggregate;

static class AggregateInfoBuilder
{
	public static GeneratorResult<AggregateInfo> Build(
		INamedTypeSymbol classSymbol,
		ClassDeclarationSyntax syntax,
		Compilation compilation,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var classMetadataFullName = classSymbol.ContainingNamespace.IsGlobalNamespace
			? classSymbol.MetadataName
			: $"{classSymbol.ContainingNamespace}.{classSymbol.MetadataName}";
		var mergedClassSymbol = compilation.GetTypeByMetadataName(classMetadataFullName);
		if (mergedClassSymbol is not null)
			classSymbol = mergedClassSymbol;
		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

		var canGenerate = ValidateAggregateClass(
			classSymbol,
			syntax,
			diagnostics,
			out var shouldDeclareAggregateBase,
			out var isPartial,
			out var inheritsAggregateBase
		);

		var hasManualRegisterEvents = AggregateEventMethodBuilder.HasRegisterEventsMethod(classSymbol, out _);

		TypeIdentity aggregateType = new(classSymbol);
		var aggregateAttribute = AggregateAttributeData.FromAttributeData(classSymbol);
		var assemblyDefaults = AggregateDefaultsAttributeData.FromAttributeData(compilation.Assembly);

		var aggregateNamespace = aggregateType.Namespace;
		var eventNamespaceOverride = aggregateAttribute.Exists ? aggregateAttribute.EventNamespace : null;
		var aggregateEventSuffixOverride = aggregateAttribute.Exists ? aggregateAttribute.EventSuffix : null;
		var assemblyEventSuffix = assemblyDefaults.Exists ? assemblyDefaults.EventSuffix : null;
		var valueObjectContextType = compilation.GetTypeByMetadataName(
			"Purview.EventSourcing.ValueObjects.ValueObjectContext`1"
		);

		List<AggregateStatePropertyInfo> properties = [];
		Dictionary<string, IPropertySymbol> propertySymbolsByName = new(StringComparer.Ordinal);
		List<IMethodSymbol> attributedMethods = [];

		ScanProperties(
			classSymbol,
			compilation,
			diagnostics,
			properties,
			propertySymbolsByName,
			attributedMethods,
			cancellationToken
		);

		var methods = new List<AggregateEventMethodInfo>();
		var invalidMethods = new List<InvalidAggregateEventMethodInfo>();
		var methodsByEventType = new Dictionary<TypeReference, IMethodSymbol>();
		var methodsBySchemaVersion = new Dictionary<int, (IMethodSymbol Symbol, bool IsExplicit)>();

		BuildMethods(
			classSymbol,
			compilation,
			valueObjectContextType,
			aggregateNamespace,
			eventNamespaceOverride,
			aggregateEventSuffixOverride,
			assemblyEventSuffix,
			propertySymbolsByName,
			attributedMethods,
			methods,
			invalidMethods,
			methodsByEventType,
			methodsBySchemaVersion,
			diagnostics,
			cancellationToken
		);

		var containingTypes = new List<AggregateContainingTypeInfo>();
		var currentContainingType = classSymbol.ContainingType;
		while (currentContainingType is not null)
		{
			var containingTypeParameters = currentContainingType
				.TypeParameters.Select(static tp => new GenericTypeParameterOptions(tp.Name))
				.ToImmutableArray();

			containingTypes.Insert(
				0,
				new AggregateContainingTypeInfo(
					currentContainingType.Name,
					currentContainingType.DeclaredAccessibility,
					currentContainingType.IsStatic,
					containingTypeParameters
				)
			);

			currentContainingType = currentContainingType.ContainingType;
		}

		var aggregateTypeParameters = classSymbol
			.TypeParameters.Select(static tp => new GenericTypeParameterOptions(tp.Name))
			.ToImmutableArray();

		return GeneratorResult<AggregateInfo>.Create(
			new(
				aggregateType,
				classSymbol.DeclaredAccessibility,
				shouldDeclareAggregateBase,
				properties.ToImmutableArray(),
				methods.ToImmutableArray(),
				invalidMethods.ToImmutableArray(),
				AggregateEventMethodBuilder.CreateHintName(classSymbol),
				canGenerate,
				isPartial,
				inheritsAggregateBase,
				hasManualRegisterEvents,
				containingTypes.ToImmutableArray(),
				aggregateTypeParameters
			),
			diagnostics.ToImmutable()
		);
	}

	static bool ValidateAggregateClass(
		INamedTypeSymbol classSymbol,
		ClassDeclarationSyntax syntax,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		out bool shouldDeclareAggregateBase,
		out bool isPartial,
		out bool inheritsAggregateBase
	)
	{
		shouldDeclareAggregateBase = false;
		isPartial = TypeHelpers.IsPartial(syntax);
		inheritsAggregateBase = TypeHelpers.InheritsFrom(
			classSymbol,
			TypeLibrary.Purview.EventSourcing.Aggregates.AggregateBase
		);
		var canGenerate = true;

		if (!isPartial)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.AggregateMustBePartial,
					syntax.Identifier.GetLocation(),
					classSymbol.Name
				)
			);
			canGenerate = false;
		}

		if (classSymbol.ContainingType is not null)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.NestedAggregatesAreNotSupported,
					syntax.Identifier.GetLocation(),
					classSymbol.Name
				)
			);
			canGenerate = false;
		}

		if (classSymbol.TypeParameters.Length > 0)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.GenericAggregatesAreNotSupported,
					syntax.Identifier.GetLocation(),
					classSymbol.Name
				)
			);
			canGenerate = false;
		}

		if (!inheritsAggregateBase)
		{
			if (classSymbol.BaseType is null || classSymbol.BaseType.SpecialType == SpecialType.System_Object)
			{
				shouldDeclareAggregateBase = true;
			}
			else
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.AggregateMustInheritAggregateBase,
						syntax.Identifier.GetLocation(),
						classSymbol.Name
					)
				);
				canGenerate = false;
			}
		}

		if (AggregateEventMethodBuilder.HasRegisterEventsMethod(classSymbol, out var registerEventsMethod))
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.ManualRegisterEventsIsNotSupported,
					registerEventsMethod!.Locations.FirstOrDefault(),
					classSymbol.Name
				)
			);
			canGenerate = false;
		}

		return canGenerate;
	}

	static void ScanProperties(
		INamedTypeSymbol classSymbol,
		Compilation compilation,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		List<AggregateStatePropertyInfo> properties,
		Dictionary<string, IPropertySymbol> propertySymbolsByName,
		List<IMethodSymbol> attributedMethods,
		CancellationToken cancellationToken
	)
	{
		foreach (var member in classSymbol.GetMembers())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (member is IPropertySymbol propertySymbol)
			{
				if (propertySymbol.IsStatic || propertySymbol.IsIndexer || propertySymbol.IsImplicitlyDeclared)
					continue;

				if (
					AggregateEventMethodBuilder.TryGetComplexScalarValueType(
						propertySymbol.Type,
						out var scalarValueTypeDisplayName
					)
				)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.ScalarComplexValueMayNotTranslateInSqlSnapshots,
							propertySymbol.Locations.FirstOrDefault(),
							propertySymbol.Name,
							classSymbol.Name,
							scalarValueTypeDisplayName
						)
					);
				}

				propertySymbolsByName[propertySymbol.Name] = propertySymbol;

				if (propertySymbol.SetMethod is null)
					continue;

				if (propertySymbol.SetMethod.DeclaredAccessibility is not Accessibility.Private)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.AggregatePropertySetterShouldBePrivate,
							propertySymbol.SetMethod.Locations.FirstOrDefault()
								?? propertySymbol.Locations.FirstOrDefault(),
							propertySymbol.Name,
							classSymbol.Name,
							propertySymbol.SetMethod.DeclaredAccessibility.ToString()
						)
					);
				}

				if (
					AggregateEventMethodBuilder.IsCollectionLikeType(propertySymbol.Type)
					&& !AggregateEventMethodBuilder.IsEventStoreCollectionType(propertySymbol.Type)
				)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.AggregatePropertyCollectionTypeMustUseEventStoreCollections,
							propertySymbol.Locations.FirstOrDefault(),
							propertySymbol.Name,
							classSymbol.Name,
							propertySymbol.Type.ToDisplayString()
						)
					);
				}

				properties.Add(
					new(
						propertySymbol.Name,
						AggregateEventMethodBuilder.CreateTypeReference(propertySymbol.Type, compilation)
					)
				);
				continue;
			}

			if (
				member is IMethodSymbol methodSymbol
				&& (
					AggregateEventMethodBuilder.HasAttribute(
						methodSymbol,
						TypeLibrary.Purview.EventSourcing.Aggregates.EventAttribute
					)
					|| AggregateEventMethodBuilder.HasAttribute(
						methodSymbol,
						TypeLibrary.Purview.EventSourcing.Aggregates.CollectionEventAttribute
					)
				)
			)
			{
				attributedMethods.Add(methodSymbol);
			}
		}
	}

	static void BuildMethods(
		INamedTypeSymbol classSymbol,
		Compilation compilation,
		INamedTypeSymbol? valueObjectContextType,
		string? aggregateNamespace,
		string? eventNamespaceOverride,
		string? aggregateEventSuffixOverride,
		string? assemblyEventSuffix,
		Dictionary<string, IPropertySymbol> propertySymbolsByName,
		List<IMethodSymbol> attributedMethods,
		List<AggregateEventMethodInfo> methods,
		List<InvalidAggregateEventMethodInfo> invalidMethods,
		Dictionary<TypeReference, IMethodSymbol> methodsByEventType,
		Dictionary<int, (IMethodSymbol Symbol, bool IsExplicit)> methodsBySchemaVersion,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		CancellationToken cancellationToken
	)
	{
		foreach (var methodSymbol in attributedMethods)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var diagnosticsStart = diagnostics.Count;

			if (
				!AggregateEventMethodBuilder.TryBuild(
					classSymbol,
					methodSymbol,
					propertySymbolsByName,
					compilation,
					valueObjectContextType,
					aggregateNamespace,
					eventNamespaceOverride,
					aggregateEventSuffixOverride,
					assemblyEventSuffix,
					diagnostics,
					cancellationToken,
					out var methodInfo
				)
			)
			{
				var diagnosticIds = diagnostics
					.Skip(diagnosticsStart)
					.Select(static diagnostic => diagnostic.Descriptor.Id)
					.Distinct(StringComparer.Ordinal)
					.OrderBy(static id => id, StringComparer.Ordinal)
					.ToArray();

				if (
					AggregateEventMethodBuilder.TryCreateInvalidMethodStub(
						methodSymbol,
						diagnosticIds,
						out var invalidMethod,
						cancellationToken
					)
				)
					invalidMethods.Add(invalidMethod);

				continue;
			}

			if (methodsByEventType.TryGetValue(methodInfo.EventType, out var conflictingMethod))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.DuplicateGeneratedEventName,
						methodSymbol,
						methodSymbol.Name,
						classSymbol.Name,
						methodInfo.EventType.Identity.Name
					)
				);
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.DuplicateGeneratedEventName,
						conflictingMethod,
						conflictingMethod.Name,
						classSymbol.Name,
						methodInfo.EventType.Identity.Name
					)
				);

				if (
					AggregateEventMethodBuilder.TryCreateInvalidMethodStub(
						methodSymbol,
						[DiagnosticLibrary.DuplicateGeneratedEventName.Id],
						out var invalidMethod,
						cancellationToken
					)
				)
					invalidMethods.Add(invalidMethod);

				continue;
			}

			if (methodsBySchemaVersion.TryGetValue(methodInfo.Version, out var existingSchemaVersionMethod))
			{
				if (methodInfo.Version > 1 && existingSchemaVersionMethod.IsExplicit)
				{
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.DuplicateEventSchemaVersionOnAggregate,
							methodSymbol,
							methodSymbol.Name,
							classSymbol.Name,
							$"{methodInfo.Version}",
							existingSchemaVersionMethod.Symbol.Name
						)
					);
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.DuplicateEventSchemaVersionOnAggregate,
							existingSchemaVersionMethod.Symbol,
							existingSchemaVersionMethod.Symbol.Name,
							classSymbol.Name,
							methodInfo.Version,
							methodSymbol.Name
						)
					);

					if (
						AggregateEventMethodBuilder.TryCreateInvalidMethodStub(
							methodSymbol,
							[DiagnosticLibrary.DuplicateEventSchemaVersionOnAggregate.Id],
							out var invalidMethod,
							cancellationToken
						)
					)
						invalidMethods.Add(invalidMethod);

					continue;
				}

				if (methodInfo.Version > 1 && !existingSchemaVersionMethod.IsExplicit)
					methodsBySchemaVersion[methodInfo.Version] = (methodSymbol, true);
			}
			else
			{
				methodsBySchemaVersion[methodInfo.Version] = (methodSymbol, methodInfo.Version > 1);
			}

			methodsByEventType[methodInfo.EventType] = methodSymbol;
			methods.Add(methodInfo);
		}
	}
}
