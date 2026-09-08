namespace Purview.EventSourcing.SourceGenerator.Aggregate;

partial class AggregateSourceEmitter
{
	static void GenerateEventClass(
		AggregateEmitContext outputContext,
		CodeWriter writer,
		AggregateEventMethodInfo method
	)
	{
		outputContext.Debug(
			$"Generating event class '{method.EventType}' for method '{method.MethodName}' with {method.EventParameters.Count} stored parameters and version {method.Version}."
		);

		var hashParameterName = method.EventParameters.IsEmpty ? "_" : "hash";

		writer.Class(
			new(method.EventType.Identity.Name, TypeDeclarationAccessibility.Public)
			{
				IsSealed = true,
				IsPartial = false,
				BaseType = TypeLibrary.Purview.EventSourcing.Aggregates.Events.EventBase,
			},
			bodyWriter =>
			{
				foreach (var prop in method.EventParameters)
				{
					var propertyType = prop.PropertyType;

					bodyWriter.Property(
						new(prop.PropertyName, propertyType, TypeDeclarationAccessibility.Public)
						{
							HasSetter = true,
							Initializer = "default!",
						}
					);
				}

				bodyWriter.Property(
					new("SchemaVersion", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
					{
						IsOverride = true,
						ExpressionBody = $"{method.Version}",
					}
				);

				bodyWriter.Method(
					new("BuildEventHash", TypeDeclarationAccessibility.Protected)
					{
						IsOverride = true,
						Parameters = [new(hashParameterName, TypeLibrary.System.HashCode, ParameterModifier.Ref)],
					},
					methodBodyWriter =>
					{
						foreach (var prop in method.EventParameters)
							methodBodyWriter.MethodCallOn(hashParameterName, "Add", prop.PropertyName);
					}
				);
			}
		);
	}
}
