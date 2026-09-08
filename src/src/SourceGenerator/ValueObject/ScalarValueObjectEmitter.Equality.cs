namespace Purview.EventSourcing.SourceGenerator.ValueObject;

static partial class ScalarValueObjectEmitter
{
	static void EmitEquality(CodeWriter writer, ScalarValueObjectModel model)
	{
		if (!model.EqualsSelfExists)
		{
			writer.MethodExpression(
				new("Equals", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("other", ValueObjectType(model))],
					ExpressionBody = model.IsReferenceType
						? $"other is not null && global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.Equals({model.ScalarPropertyName}, other.{model.ScalarPropertyName})"
						: $"global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.Equals({model.ScalarPropertyName}, other.{model.ScalarPropertyName})",
				}
			);
		}

		if (!model.EqualsPrimitiveExists)
		{
			writer.MethodExpression(
				new("Equals", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("other", model.ScalarTypeReference)],
					ExpressionBody =
						$"global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.Equals({model.ScalarPropertyName}, other)",
				}
			);
		}

		if (!model.EqualsObjectExists)
		{
			writer.MethodExpression(
				new("Equals", PurviewTypeLibrary.System.Boolean, TypeDeclarationAccessibility.Public)
				{
					IsOverride = true,
					Parameters = [new("obj", PurviewTypeLibrary.System.Object.MakeNullable(writer))],
					ExpressionBody =
						$"obj is {model.TypeName} other ? Equals(other) : obj is {model.ScalarTypeName} primitive && Equals(primitive)",
				}
			);
		}

		if (!model.GetHashCodeExists)
		{
			writer.MethodExpression(
				new("GetHashCode", PurviewTypeLibrary.System.Int32, TypeDeclarationAccessibility.Public)
				{
					IsOverride = true,
					ExpressionBody =
						$"global::System.Collections.Generic.EqualityComparer<{model.ScalarTypeName}>.Default.GetHashCode({model.ScalarPropertyName})",
				}
			);
		}
	}

	static void EmitOperators(CodeWriter writer, ScalarValueObjectModel model)
	{
		var valueObjectType = ValueObjectType(model);

		if (!model.SameTypeEqualityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				valueObjectType,
				valueObjectType,
				"==",
				model.IsReferenceType ? "left is null ? right is null : left.Equals(right)" : "left.Equals(right)"
			);
		}

		if (!model.SameTypeInequalityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				valueObjectType,
				valueObjectType,
				"!=",
				"!(left == right)"
			);
		}

		if (!model.PrimitiveEqualityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				valueObjectType,
				model.ScalarTypeReference,
				"==",
				model.IsReferenceType ? "left is null ? false : left.Equals(right)" : "left.Equals(right)"
			);
		}

		if (!model.PrimitiveInequalityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				valueObjectType,
				model.ScalarTypeReference,
				"!=",
				"!(left == right)"
			);
		}

		if (!model.ReversePrimitiveEqualityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				model.ScalarTypeReference,
				valueObjectType,
				"==",
				model.IsReferenceType ? "right is not null && right.Equals(left)" : "right.Equals(left)"
			);
		}

		if (!model.ReversePrimitiveInequalityOperatorExists)
		{
			ValueObjectEmitterHelpers.EmitBinaryOperator(
				writer,
				model.ScalarTypeReference,
				valueObjectType,
				"!=",
				"!(left == right)"
			);
		}
	}

	static void EmitConversions(CodeWriter writer, ScalarValueObjectModel model)
	{
		var valueObjectType = ValueObjectType(model);

		if (model.Options.GenerateImplicitToPrimitive && !model.HasValueObjectToPrimitiveConversion)
		{
			using (
				writer.OperatorScope(
					new OperatorDeclarationOptions(
						"op_Implicit",
						model.ScalarTypeReference,
						new("valueObject", valueObjectType)
					)
					{
						Accessibility = TypeDeclarationAccessibility.Public,
						Kind = OperatorDeclarationKind.ImplicitConversion,
						ExpressionBody = $"valueObject.{model.ScalarPropertyName}",
					}
				)
			)
			{
				//
			}
		}

		if (
			model.Options.GenerateImplicitFromPrimitive
			&& !model.HasContextualCreateOverload
			&& !model.HasPrimitiveToValueObjectConversion
		)
		{
			using (
				writer.OperatorScope(
					new OperatorDeclarationOptions(
						"op_Implicit",
						valueObjectType,
						new("value", model.ScalarTypeReference)
					)
					{
						Accessibility = TypeDeclarationAccessibility.Public,
						Kind = OperatorDeclarationKind.ImplicitConversion,
						ExpressionBody = "Create(value)",
					}
				)
			)
			{
				//
			}
		}
	}

	static void EmitToString(CodeWriter writer, ScalarValueObjectModel model)
	{
		if (model.ToStringExists)
			return;

		writer.MethodExpression(
			new("ToString", PurviewTypeLibrary.System.String, TypeDeclarationAccessibility.Public)
			{
				IsOverride = true,
				ExpressionBody = $"{model.ScalarPropertyName}.ToString() ?? string.Empty",
			}
		);
	}

	static void EmitJsonConverter(CodeWriter writer, ScalarValueObjectModel model)
	{
		if (!model.Options.GenerateJsonConverter)
			return;

		var valueObjectType = ValueObjectType(model);
		var factoryMethod =
			model.Options.DeserializationMode == ValueObjectSymbolInspector.StrictModeName ? "Create" : "Hydrate";

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
							"value",
							writeValue =>
								writeValue.MethodCall(
									"Deserialize",
									[new MethodCallArgumentOptions("reader", ParameterModifier.Ref), "options"],
									receiver: $"{TypeLibrary.System.Text.Json.JsonSerializer}",
									genericArguments: [model.ScalarTypeReference]
								)
						);

						if (model.ScalarCanBeNull)
						{
							methodBody.IfBlock(
								"value is null",
								ifBody =>
									ifBody.Throw(
										TypeLibrary.System.Text.Json.JsonException,
										$"{model.TypeModel.Name} cannot be null."
									)
							);
						}

						methodBody.Return($"{factoryMethod}(value)");
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
						methodBody.MethodCallOn(
							$"{TypeLibrary.System.Text.Json.JsonSerializer}",
							"Serialize",
							"writer",
							$"value.{model.ScalarPropertyName}",
							"options"
						)
				);
			}
		);
	}
}
