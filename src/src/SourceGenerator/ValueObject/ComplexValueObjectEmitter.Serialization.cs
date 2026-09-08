namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ComplexValueObjectEmitter
{
	static void EmitJsonConverter(CodeWriter writer, ComplexValueObjectModel model)
	{
		if (!model.Options.GenerateJsonConverter)
			return;

		var modelTypeName = $"{model.TypeModel.Name}JsonModel";
		var jsonModelIdentity = new TypeIdentity(model.TypeModel.Name, model.TypeModel.Namespace).Nested(
			modelTypeName,
			0
		);
		var hydrateArgs = string.Join(", ", model.Properties.Select(static property => $"model.{property.Name}"));
		var valueObjectType = ValueObjectType(model);

		writer.Class(
			new($"{model.TypeModel.Name}JsonConverter")
			{
				IsPartial = false,
				IsSealed = true,
				BaseType = TypeLibrary.System.Text.Json.Serialization.JsonConverter.MakeGeneric(valueObjectType),
			},
			body =>
			{
				body.Method(
					new("Read", valueObjectType, TypeDeclarationAccessibility.Public)
					{
						IsOverride = true,
						Parameters =
						[
							new("reader", TypeLibrary.System.Text.Json.Utf8JsonReader, ParameterModifier.Ref),
							new("typeToConvert", PurviewTypeLibrary.System.Type),
							new("options", TypeLibrary.System.Text.Json.JsonSerializerOptions),
						],
					},
					methodBody =>
					{
						methodBody.Assignment(
							"var",
							"model",
							$"global::System.Text.Json.JsonSerializer.Deserialize<{modelTypeName}>(ref reader, options)"
						);
						methodBody.IfBlock(
							"model is null",
							ifBody =>
								ifBody.Throw(
									TypeLibrary.System.Text.Json.JsonException,
									$"Unable to deserialize {model.TypeModel.Name}."
								)
						);
						methodBody.Return($"{model.HydrateFactoryName}({hydrateArgs})");
					}
				);

				body.Method(
					new("Write", TypeDeclarationAccessibility.Public)
					{
						IsOverride = true,
						Parameters =
						[
							new("writer", TypeLibrary.System.Text.Json.Utf8JsonWriter),
							new("value", valueObjectType),
							new("options", TypeLibrary.System.Text.Json.JsonSerializerOptions),
						],
					},
					methodBody =>
					{
						methodBody.Assignment(
							"var",
							"model",
							new ObjectCreationOptions(jsonModelIdentity)
							{
								WriteInitializerMembersOnSeparateLines = false,
								InitializerMembers =
								[
									.. model.Properties.Select(static property => new ObjectInitializerMemberOptions(
										property.Name,
										$"value.{property.Name}"
									)),
								],
							}
						);
						methodBody.MethodCallOn(
							$"{TypeLibrary.System.Text.Json.JsonSerializer}",
							"Serialize",
							"writer",
							"model",
							"options"
						);
					}
				);
			}
		);

		writer.Class(
			new(modelTypeName) { IsPartial = false, IsSealed = true },
			body =>
			{
				foreach (var property in model.Properties)
				{
					body.Property(
						new(property.Name, property.Type, TypeDeclarationAccessibility.Public)
						{
							HasSetter = true,
							Initializer = "default!",
						}
					);
				}
			}
		);
	}
}
