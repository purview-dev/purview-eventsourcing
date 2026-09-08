using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.EventSourcing.SourceGenerator.Generators;

public sealed class ValueObjectSourceGeneratorTests : ValueObjectSourceGeneratorTestBase
{
	[Test]
	public async Task ScalarGeneration_UsesStrictCreateAndHydrateCorrectly(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{

			[Purview.EventSourcing.Serialization.Scalar]
			public readonly partial record struct EmailAddress
			{
				public string Value { get; }

				private EmailAddress(string value) => Value = value;

				static partial void OnNormalize(ref string value)
				{
					value = value?.Trim().ToLowerInvariant()!;
				}

				static partial void OnValidate(string value)
				{
					if (string.IsNullOrWhiteSpace(value))
						throw new System.ArgumentException("Email address cannot be empty.", nameof(value));

					if (!value.Contains("@", System.StringComparison.Ordinal))
						throw new System.ArgumentException("Invalid email address format.", nameof(value));
				}
			}

			public static class ValueObjectHarness
			{
				public static string StrictCreate() => EmailAddress.Create(" TEST@Example.COM ").Value;

				public static string HydratePreserves() => EmailAddress.Hydrate(" TEST@Example.COM ").Value;

				public static string HydrateInvalid() => EmailAddress.Hydrate("not-an-email").Value;

				public static bool TryCreateInvalid() => EmailAddress.TryCreate("not-an-email", out _);

				public static string SerializeEmail() => System.Text.Json.JsonSerializer.Serialize(EmailAddress.Create("test@example.com"));

				public static string DeserializeEmail() => System.Text.Json.JsonSerializer.Deserialize<EmailAddress>("\"not-an-email\"").Value;

				public static string ImplicitFromPrimitive()
				{
					EmailAddress email = " TEST@Example.COM ";
					return email.Value;
				}

				public static string ImplicitToPrimitive()
				{
					string value = EmailAddress.Create("test@example.com");
					return value;
				}

				public static bool EqualsPrimitive() => EmailAddress.Create("test@example.com").Equals("test@example.com");

				public static bool OperatorEqualsPrimitive() =>
					EmailAddress.Create("test@example.com") == "test@example.com";

				public static bool OperatorEqualsPrimitiveReverse() =>
					"test@example.com" == EmailAddress.Create("test@example.com");

				public static int CompareWithPrimitive() => EmailAddress.Create("b@example.com").CompareTo("a@example.com");

				public static int CompareWithObject() => EmailAddress.Create("a@example.com").CompareTo((object)EmailAddress.Create("b@example.com"));
			}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var harnessType = assembly.GetType("Testing.ValueObjectHarness")!;

		var strictCreate = (string)harnessType.GetMethod("StrictCreate")!.Invoke(null, null)!;
		var hydratePreserves = (string)harnessType.GetMethod("HydratePreserves")!.Invoke(null, null)!;
		var hydrateInvalid = (string)harnessType.GetMethod("HydrateInvalid")!.Invoke(null, null)!;
		var tryCreateInvalid = (bool)harnessType.GetMethod("TryCreateInvalid")!.Invoke(null, null)!;
		var serialized = (string)harnessType.GetMethod("SerializeEmail")!.Invoke(null, null)!;
		var deserialized = (string)harnessType.GetMethod("DeserializeEmail")!.Invoke(null, null)!;
		var implicitFrom = (string)harnessType.GetMethod("ImplicitFromPrimitive")!.Invoke(null, null)!;
		var implicitTo = (string)harnessType.GetMethod("ImplicitToPrimitive")!.Invoke(null, null)!;
		var equalsPrimitive = (bool)harnessType.GetMethod("EqualsPrimitive")!.Invoke(null, null)!;
		var operatorEqualsPrimitive = (bool)harnessType.GetMethod("OperatorEqualsPrimitive")!.Invoke(null, null)!;
		var operatorEqualsPrimitiveReverse = (bool)
			harnessType.GetMethod("OperatorEqualsPrimitiveReverse")!.Invoke(null, null)!;
		var comparePrimitive = (int)harnessType.GetMethod("CompareWithPrimitive")!.Invoke(null, null)!;
		var compareObject = (int)harnessType.GetMethod("CompareWithObject")!.Invoke(null, null)!;

		await Assert.That(strictCreate).IsEqualTo("test@example.com");
		await Assert.That(hydratePreserves).IsEqualTo(" TEST@Example.COM ");
		await Assert.That(hydrateInvalid).IsEqualTo("not-an-email");
		await Assert.That(tryCreateInvalid).IsFalse();
		await Assert.That(serialized).IsEqualTo("\"test@example.com\"");
		await Assert.That(deserialized).IsEqualTo("not-an-email");
		await Assert.That(implicitFrom).IsEqualTo("test@example.com");
		await Assert.That(implicitTo).IsEqualTo("test@example.com");
		await Assert.That(equalsPrimitive).IsTrue();
		await Assert.That(operatorEqualsPrimitive).IsTrue();
		await Assert.That(operatorEqualsPrimitiveReverse).IsTrue();
		await Assert.That(comparePrimitive).IsEqualTo(1);
		await Assert.That(compareObject).IsEqualTo(-1);
	}

	[Test]
	public async Task Scalar_ClassReferenceType_GeneratesNullableCompareToSelfSignature(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar]
				public sealed partial record BlobUri
				{
					public string Value { get; }

					static partial void OnValidate(string value)
					{
						System.ArgumentNullException.ThrowIfNull(value);
					}

					public static BlobUri Empty => Hydrate(null!);
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var query = result.Generated();
		var blobUri = query.GetRecord("BlobUri", "Testing");
		var compareTo = blobUri.GetMethod("CompareTo", TypeRefs.Named("BlobUri", "Testing"));
		await Assert.That(compareTo.Node.ParameterList.Parameters[0].Type?.ToString()).Contains("?");

		var errors = result
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(static d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();
		await Assert.That(errors).IsEmpty();
	}

	[Test]
	public async Task ScalarGeneration_GeneratesPrivateConstructorWhenMissing(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{

				[Purview.EventSourcing.Serialization.Scalar]
				public readonly partial record struct PhoneNumber
				{
					public string Value { get; }
				}

				public static class PhoneHarness
				{
					public static string CreatePhone() => PhoneNumber.Create("12345").Value;

					public static string HydratePhone() => PhoneNumber.Hydrate("67890").Value;
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var query = result.Generated();
		var phoneNumber = query.GetRecord("PhoneNumber", "Testing");
		var ctor = phoneNumber.GetConstructor(TypeRefs.String);
		await Assert.That(ctor.Node.Modifiers.ToString()).Contains("private");
		await Assert.That(result).DoesNotHaveDiagnostic(DiagnosticLibrary.ScalarConstructorMissing);

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var harnessType = assembly.GetType("Testing.PhoneHarness")!;

		var created = (string)harnessType.GetMethod("CreatePhone")!.Invoke(null, null)!;
		var hydrated = (string)harnessType.GetMethod("HydratePhone")!.Invoke(null, null)!;

		await Assert.That(created).IsEqualTo("12345");
		await Assert.That(hydrated).IsEqualTo("67890");
	}

	[Test]
	public async Task ScalarGeneration_GeneratesEmptyByDefault(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar]
				public sealed partial record BlobUri
				{
					public string Value { get; }
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var query = result.Generated();
		var blobUri = query.GetRecord("BlobUri", "Testing");
		var empty = blobUri.GetProperty("Empty", TypeRefs.Named("BlobUri", "Testing"));
		await Assert.That(empty.Node.Modifiers.ToString()).Contains("static");
	}

	[Test]
	public async Task ScalarRecordStruct_GeneratesToStringOverride(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar]
				public readonly partial record struct EmailAddress
				{
					public string Value { get; }

					public string Domain => Value.Split('@')[1];

					private EmailAddress(string value) => Value = value;
				}

				public static class EmailToStringHarness
				{
					public static string ToStringValue() => EmailAddress.Create("dan.moore@example.com").ToString();
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var query = result.Generated();
		var emailAddress = query.GetRecord("EmailAddress", "Testing");
		var toString = emailAddress.GetMethod("ToString");
		await Assert.That(toString.Node.Modifiers.ToString()).Contains("override");

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var harnessType = assembly.GetType("Testing.EmailToStringHarness")!;
		var toStringValue = (string)harnessType.GetMethod("ToStringValue")!.Invoke(null, null)!;

		await Assert.That(toStringValue).IsEqualTo("dan.moore@example.com");
	}

	[Test]
	public async Task ScalarGeneration_CanDisableEmpty(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar(GenerateEmpty = false)]
				public sealed partial record BlobUri
				{
					public string Value { get; }
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result.Generated().GetRecord("BlobUri", "Testing").HasProperty("Empty")).IsFalse();
	}

	[Test]
	public async Task ScalarGeneration_GeneratesPrivateConstructorOnIContextualValueObjectWhenMissing(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				public class ProjectAggregate : AggregateBase
				{
					public ProjectId Id { get; }

					protected override void RegisterEvents()
					{
					}
				}

				[Scalar]
				public readonly partial record struct ProjectId : IContextualValueObject<ProjectId, string, ProjectAggregate>
				{
					public string Value { get; }

					static partial void OnValidate(string value)
					{
						if (!Guid.TryParse(value, out var parsedValue))
							throw new ArgumentException("ProjectId must be a valid GUID.", nameof(value));

						if (parsedValue == Guid.Empty)
							throw new ArgumentException("ProjectId cannot be empty.", nameof(value));
					}

					public static ProjectId Create(string value, in ValueObjectContext<ProjectAggregate> context) => new(value);
				}

				public static class ProjectHarness
				{
					public static string CreateProjectId() => ProjectId.Create("5801da4a-ed0f-46de-ba9d-5b6adda6e917").Value;

					public static string HydrateProjectId() => ProjectId.Hydrate("6801da4a-ed0f-46de-ba9d-5b6adda6e917").Value;
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var query = result.Generated();
		var projectId = query.GetRecord("ProjectId", "Testing");
		var ctor = projectId.GetConstructor(TypeRefs.String);
		await Assert.That(ctor.Node.Modifiers.ToString()).Contains("private");

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();
		var harnessType = assembly.GetType("Testing.ProjectHarness")!;

		var created = (string)harnessType.GetMethod("CreateProjectId")!.Invoke(null, null)!;
		var hydrated = (string)harnessType.GetMethod("HydrateProjectId")!.Invoke(null, null)!;

		await Assert.That(created).IsEqualTo("5801da4a-ed0f-46de-ba9d-5b6adda6e917");
		await Assert.That(hydrated).IsEqualTo("6801da4a-ed0f-46de-ba9d-5b6adda6e917");
	}

	[Test]
	public async Task ScalarGeneration_WarnsWhenScalarStructIsNotRecordStruct(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar]
				public readonly partial struct LegacyStatus
				{
					public string Value { get; }

					private LegacyStatus(string value) => Value = value;
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ScalarShouldBeRecordStruct);
	}

	[Test]
	public async Task ScalarGeneration_GivenInvalidScalar_WithGeneratorOnly_ProducesNoOutputAndNoExceptions(
		CancellationToken cancellationToken
	)
	{
		// The generator consumes the same shared validation as the analyzer: a scalar missing its
		// Value property must be skipped entirely. Diagnostics are analyzer-owned, so a generator-only
		// run reports nothing but must not throw or generate an invalid type.
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar]
				public readonly partial record struct BrokenScalar
				{
				}
			}
			""";

		var generatorOnlyOptions = EventSourcingGeneratorTestOptions.Default with { AnalyzerTypes = [] };
		var result = await GenerateAsync(source, generatorOnlyOptions, cancellationToken);

		await Assert
			.That(result.AllSyntaxTrees.Length)
			.IsEqualTo(EventSourcingGeneratorTestOptions.ValueObjectExpectedFileCount);
		foreach (var genResult in result.DriverResult.Results)
			await Assert.That(genResult.Exception).IsNull();
		await Assert.That(result.Generated().HasRecord("BrokenScalar", "Testing")).IsFalse();
	}

	[Test]
	public async Task ScalarGeneration_GivenInvalidScalar_AnalyzerReportsAndGeneratorSkips(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar]
				public readonly partial record struct BrokenScalar
				{
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result).HasDiagnostic(DiagnosticLibrary.ScalarPropertyMissing);
		await Assert
			.That(result.AllSyntaxTrees.Length)
			.IsEqualTo(EventSourcingGeneratorTestOptions.ValueObjectExpectedFileCount);
		await Assert.That(result.Generated().HasRecord("BrokenScalar", "Testing")).IsFalse();
	}

	[Test]
	public async Task ScalarGeneration_GeneratesEnumConvenienceProperties(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar]
				public readonly partial record struct ReportProcessingStatus
				{
					public ReportProcessingStatusCode Value { get; }

					private ReportProcessingStatus(ReportProcessingStatusCode value) => Value = value;
				}

				public enum ReportProcessingStatusCode
				{
					Uploaded,
					Processing,
					Completed,
					Failed
				}

				public static class StatusHarness
				{
					public static bool AreEqual() =>
						ReportProcessingStatus.Failed == ReportProcessingStatus.Hydrate(ReportProcessingStatusCode.Failed);
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var query = result.Generated();
		var reportProcessingStatus = query.GetRecord("ReportProcessingStatus", "Testing");
		var statusType = TypeRefs.Named("ReportProcessingStatus", "Testing");
		await Assert.That(reportProcessingStatus.HasProperty("Uploaded", statusType)).IsTrue();
		await Assert.That(reportProcessingStatus.HasProperty("Processing", statusType)).IsTrue();
		await Assert.That(reportProcessingStatus.HasProperty("Completed", statusType)).IsTrue();
		await Assert.That(reportProcessingStatus.HasProperty("Failed", statusType)).IsTrue();

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var harnessType = assembly.GetType("Testing.StatusHarness")!;
		var areEqual = (bool)harnessType.GetMethod("AreEqual")!.Invoke(null, null)!;

		await Assert.That(areEqual).IsTrue();
	}

	[Test]
	public async Task ScalarGeneration_CanDisableEnumConvenienceProperties(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar(GenerateEnumProperties = false)]
				public readonly partial record struct ReportProcessingStatus
				{
					public ReportProcessingStatusCode Value { get; }

					private ReportProcessingStatus(ReportProcessingStatusCode value) => Value = value;
				}

				public enum ReportProcessingStatusCode
				{
					Uploaded,
					Processing,
					Completed,
					Failed
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var query = result.Generated();
		var reportProcessingStatus = query.GetRecord("ReportProcessingStatus", "Testing");
		await Assert.That(reportProcessingStatus.HasProperty("Uploaded")).IsFalse();
		await Assert.That(reportProcessingStatus.HasProperty("Processing")).IsFalse();
		await Assert.That(reportProcessingStatus.HasProperty("Completed")).IsFalse();
		await Assert.That(reportProcessingStatus.HasProperty("Failed")).IsFalse();
	}

	[Test]
	public async Task ComplexValueObjectGeneration_UsesObjectShapedJson(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{

			[Purview.EventSourcing.Serialization.Scalar]
			public readonly partial record struct CurrencyCode
			{
				public string Value { get; }

				private CurrencyCode(string value) => Value = value;

				static partial void OnNormalize(ref string value)
				{
					value = value?.Trim().ToUpperInvariant()!;
				}

				static partial void OnValidate(string value)
				{
					if (string.IsNullOrWhiteSpace(value) || value.Length != 3)
						throw new System.ArgumentException("Invalid currency code.", nameof(value));
				}
			}

			[Purview.EventSourcing.Serialization.ValueObject]
			public readonly partial record struct Money
			{
				public decimal Amount { get; }

				public CurrencyCode Currency { get; }

				private Money(decimal amount, CurrencyCode currency)
				{
					Amount = amount;
					Currency = currency;
				}

				public static Money Create(decimal amount, CurrencyCode currency)
				{
					if (amount < 0)
						throw new System.ArgumentOutOfRangeException(nameof(amount));

					return new(amount, currency);
				}
			}

			public static class ComplexHarness
			{
				public static string SerializeMoney()
				{
					var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
					return System.Text.Json.JsonSerializer.Serialize(Money.Create(10.50m, CurrencyCode.Create("GBP")), options);
				}

				public static decimal DeserializeAmount()
				{
					var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
					var money = System.Text.Json.JsonSerializer.Deserialize<Money>("{\"amount\":10.5,\"currency\":\"GBP\"}", options);
					return money.Amount;
				}

				public static int CompareMoney() => Money.Create(10.5m, CurrencyCode.Create("GBP")).CompareTo(Money.Create(11m, CurrencyCode.Create("GBP")));
			}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var harnessType = assembly.GetType("Testing.ComplexHarness")!;

		var json = (string)harnessType.GetMethod("SerializeMoney")!.Invoke(null, null)!;
		var amount = (decimal)harnessType.GetMethod("DeserializeAmount")!.Invoke(null, null)!;
		var compareResult = (int)harnessType.GetMethod("CompareMoney")!.Invoke(null, null)!;

		await Assert.That(json).Contains("\"amount\"");
		await Assert.That(json).Contains("\"currency\"");
		await Assert.That(amount).IsEqualTo(10.5m);
		await Assert.That(compareResult).IsEqualTo(-1);
	}

	[Test]
	public async Task ComplexValueObjectGeneration_GeneratesPrivateConstructorAndEqualityWhenMissing(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{

			[Purview.EventSourcing.Serialization.ValueObject]
			public partial class Address
			{
				public string Line1 { get; }

				public string City { get; }
			}

			public static class AddressHarness
			{
				public static bool AreEqual() => Address.Hydrate("1 Example Street", "London") == Address.Hydrate("1 Example Street", "London");

				public static string City() => Address.Hydrate("1 Example Street", "London").City;
			}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var query = result.Generated();
		var address = query.GetClass("Address", "Testing");
		var addressType = TypeRefs.Named("Address", "Testing");

		var ctor = address.GetConstructor(TypeRefs.String, TypeRefs.String);
		await Assert.That(ctor.Node.Modifiers.ToString()).Contains("private");
		await Assert.That(address.HasMethod("Equals", addressType)).IsTrue();
		await Assert.That(HasOperator(query, address, "==", "Address", "Address")).IsTrue();

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();
		var harnessType = assembly.GetType("Testing.AddressHarness")!;

		var areEqual = (bool)harnessType.GetMethod("AreEqual")!.Invoke(null, null)!;
		var city = (string)harnessType.GetMethod("City")!.Invoke(null, null)!;

		await Assert.That(areEqual).IsTrue();
		await Assert.That(city).IsEqualTo("London");
	}

	[Test]
	public async Task ComplexValueObjectGeneration_GeneratesEmptyByDefault(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.ValueObject]
				public partial class UserDetails
				{
					public System.Guid Id { get; }

					public string? Name { get; }

					public bool IsActive { get; }
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var query = result.Generated();
		var userDetails = query.GetClass("UserDetails", "Testing");
		var empty = userDetails.GetProperty("Empty", TypeRefs.Named("UserDetails", "Testing"));
		await Assert.That(empty.Node.Modifiers.ToString()).Contains("static");
	}

	[Test]
	public async Task ComplexValueObjectGeneration_PlainStructImplementsSelfEquatable(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.ValueObject]
				public partial struct UserCaptureStruct
				{
					public UserDetails User { get; }

					public System.DateTimeOffset OccurredAt { get; }
				}

				[Purview.EventSourcing.Serialization.ValueObject]
				public partial class UserDetails(System.Guid id, string displayName);
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var query = result.Generated();
		var userCaptureStruct = query.GetStruct("UserCaptureStruct", "Testing");
		await Assert.That(userCaptureStruct.Node.BaseList?.ToString()).Contains("IEquatable");

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var generatedType = assembly.GetType("Testing.UserCaptureStruct")!;

		await Assert.That(typeof(IEquatable<>).MakeGenericType(generatedType).IsAssignableFrom(generatedType)).IsTrue();
	}

	[Test]
	public async Task ComplexValueObjectGeneration_WithoutProperties_GeneratesValidCreateAndJsonData(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.ValueObject]
				public partial class EmptyValueObject
				{
				}

				public static class EmptyValueObjectHarness
				{
					public static bool RoundTripsAsEmptyJson()
					{
						var value = EmptyValueObject.Create();
						var json = System.Text.Json.JsonSerializer.Serialize(value);
						var deserialized = System.Text.Json.JsonSerializer.Deserialize<EmptyValueObject>(json)!;
						return json == "{}" && value == deserialized;
					}
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var query = result.Generated();
		var emptyValueObject = query.GetClass("EmptyValueObject", "Testing");
		var create = emptyValueObject.GetMethod("Create");
		await Assert.That(create.Node.Modifiers.ToString()).Contains("static");
		var createBody = create.Node.Body?.ToString() ?? string.Empty;
		await Assert.That(createBody).Contains("OnNormalize();");
		await Assert.That(createBody).DoesNotContain("OnNormalize(ref );");

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var harnessType = assembly.GetType("Testing.EmptyValueObjectHarness")!;
		var roundTripsAsEmptyJson = (bool)harnessType.GetMethod("RoundTripsAsEmptyJson")!.Invoke(null, null)!;

		await Assert.That(roundTripsAsEmptyJson).IsTrue();
	}

	[Test]
	public async Task ComplexValueObjectGeneration_CanDisableEmpty(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.ValueObject(GenerateEmpty = false)]
				public partial class UserDetails
				{
					public System.Guid Id { get; }

					public string? Name { get; }

					public bool IsActive { get; }
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		await Assert.That(result.Generated().GetClass("UserDetails", "Testing").HasProperty("Empty")).IsFalse();
	}

	[Test]
	public async Task ScalarJsonStrictMode_UsesCreateOnDeserialization(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{

				[Scalar(DeserializationMode = ValueObjectDeserializationMode.Strict)]
				public readonly partial record struct StrictEmailAddress
				{
					public string Value { get; }

					private StrictEmailAddress(string value) => Value = value;

					static partial void OnValidate(string value)
					{
						if (!value.Contains("@", System.StringComparison.Ordinal))
							throw new System.ArgumentException("Invalid email address.", nameof(value));
					}
				}

				public static class StrictHarness
				{
					public static string DeserializeValid() => System.Text.Json.JsonSerializer.Deserialize<StrictEmailAddress>("\"test@example.com\"").Value;

					public static void DeserializeInvalid() => _ = System.Text.Json.JsonSerializer.Deserialize<StrictEmailAddress>("\"not-an-email\"");
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var query = result.Generated();
		var converter = query.GetClass("StrictEmailAddressJsonConverter", "Testing");
		await Assert.That(converter.GetMethod("Read").Node.Body?.ToString()).Contains("return Create(value);");

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();
		var harnessType = assembly.GetType("Testing.StrictHarness")!;

		var valid = (string)harnessType.GetMethod("DeserializeValid")!.Invoke(null, null)!;

		var threw = false;
		try
		{
			harnessType.GetMethod("DeserializeInvalid")!.Invoke(null, null);
		}
		catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
		{
			threw = true;
		}
		catch (TargetInvocationException ex)
			when (ex.InnerException is System.Text.Json.JsonException jsonException
				&& jsonException.InnerException is ArgumentException
			)
		{
			threw = true;
		}

		await Assert.That(valid).IsEqualTo("test@example.com");
		await Assert.That(threw).IsTrue();
	}

	[Test]
	public async Task ScalarComparable_GeneratesRelationalAndPrimitiveEqualityOperators(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar]
				public readonly partial record struct Name
				{
					public string Value { get; }

					private Name(string value) => Value = value;
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var query = result.Generated();
		var name = query.GetRecord("Name", "Testing");

		await Assert.That(HasOperator(query, name, "<", "Name", "Name")).IsTrue();
		await Assert.That(HasOperator(query, name, ">", "Name", "Name")).IsTrue();
		await Assert.That(HasOperator(query, name, "<=", "Name", "Name")).IsTrue();
		await Assert.That(HasOperator(query, name, ">=", "Name", "Name")).IsTrue();

		await Assert
			.That(GetOperator(query, name, "<", "Name", "Name")?.Body?.ToString())
			.Contains("return left.CompareTo(right) < 0;");
		await Assert
			.That(GetOperator(query, name, ">", "Name", "Name")?.Body?.ToString())
			.Contains("return left.CompareTo(right) > 0;");
		await Assert
			.That(GetOperator(query, name, "<=", "Name", "Name")?.Body?.ToString())
			.Contains("return left.CompareTo(right) <= 0;");
		await Assert
			.That(GetOperator(query, name, ">=", "Name", "Name")?.Body?.ToString())
			.Contains("return left.CompareTo(right) >= 0;");

		await Assert.That(HasOperator(query, name, "<", "Name", "string")).IsTrue();
		await Assert.That(HasOperator(query, name, ">", "Name", "string")).IsTrue();
		await Assert.That(HasOperator(query, name, "<=", "Name", "string")).IsTrue();
		await Assert.That(HasOperator(query, name, ">=", "Name", "string")).IsTrue();
		await Assert.That(name.HasMethod("Equals", TypeRefs.String)).IsTrue();
		await Assert.That(HasOperator(query, name, "==", "Name", "string")).IsTrue();
		await Assert.That(HasOperator(query, name, "!=", "Name", "string")).IsTrue();
		await Assert.That(HasOperator(query, name, "==", "string", "Name")).IsTrue();
		await Assert.That(HasOperator(query, name, "!=", "string", "Name")).IsTrue();

		await Assert.That(HasOperator(query, name, "<", "string", "Name")).IsFalse();
		await Assert.That(HasOperator(query, name, ">", "string", "Name")).IsFalse();
		await Assert.That(HasOperator(query, name, "<=", "string", "Name")).IsFalse();
		await Assert.That(HasOperator(query, name, ">=", "string", "Name")).IsFalse();
	}

	[Test]
	public async Task ScalarGeneration_GeneratesSelfEqualityOperatorsForPlainStruct(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar]
				public readonly partial struct ReportProcessingStatus
				{
					public ReportProcessingStatusCode Value { get; }

					private ReportProcessingStatus(ReportProcessingStatusCode value) => Value = value;
				}

				public enum ReportProcessingStatusCode
				{
					Uploaded,
					Processing,
					Completed,
					Failed
				}

				public static class StatusHarness
				{
					public static bool AreEqual()
					{
						ReportProcessingStatus status = ReportProcessingStatus.Hydrate(ReportProcessingStatusCode.Failed);
						var other = ReportProcessingStatus.Hydrate(ReportProcessingStatusCode.Failed);
						return status == other;
					}
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var query = result.Generated();
		var reportProcessingStatus = query.GetStruct("ReportProcessingStatus", "Testing");
		await Assert
			.That(HasOperator(query, reportProcessingStatus, "==", "ReportProcessingStatus", "ReportProcessingStatus"))
			.IsTrue();
		await Assert
			.That(HasOperator(query, reportProcessingStatus, "!=", "ReportProcessingStatus", "ReportProcessingStatus"))
			.IsTrue();
		await Assert.That(reportProcessingStatus.Node.BaseList?.ToString()).Contains("IEquatable");

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var harnessType = assembly.GetType("Testing.StatusHarness")!;

		var areEqual = (bool)harnessType.GetMethod("AreEqual")!.Invoke(null, null)!;

		await Assert.That(areEqual).IsTrue();
	}

	[Test]
	public async Task Scalar_GenerateComparisonOperatorsFalse_GeneratesCompareToOnly(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar(GenerateComparisonOperators = false)]
				public readonly partial record struct Name
				{
					public string Value { get; }

					private Name(string value) => Value = value;
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var query = result.Generated();
		var name = query.GetRecord("Name", "Testing");
		var nameType = TypeRefs.Named("Name", "Testing");
		await Assert.That(name.HasMethod("CompareTo", nameType)).IsTrue();
		await Assert.That(name.HasMethod("CompareTo", TypeRefs.String)).IsTrue();
		await Assert.That(HasOperator(query, name, "<", "Name", "Name")).IsFalse();
		await Assert.That(HasOperator(query, name, ">", "Name", "Name")).IsFalse();
		await Assert.That(HasOperator(query, name, "<=", "Name", "Name")).IsFalse();
		await Assert.That(HasOperator(query, name, ">=", "Name", "Name")).IsFalse();
	}

	[Test]
	public async Task Scalar_GenerateComparableFalse_SuppressesComparisonOperators(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.Scalar(GenerateComparable = false, GenerateComparisonOperators = true)]
				public readonly partial record struct Name
				{
					public string Value { get; }

					private Name(string value) => Value = value;
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var query = result.Generated();
		var name = query.GetRecord("Name", "Testing");
		var nameType = TypeRefs.Named("Name", "Testing");
		await Assert.That(name.HasMethod("CompareTo", nameType)).IsTrue();
		await Assert.That(name.HasMethod("CompareTo", TypeRefs.String)).IsTrue();
		await Assert.That(HasOperator(query, name, "<", "Name", "Name")).IsFalse();
		await Assert.That(HasOperator(query, name, ">", "Name", "Name")).IsFalse();
		await Assert.That(HasOperator(query, name, "<=", "Name", "Name")).IsFalse();
		await Assert.That(HasOperator(query, name, ">=", "Name", "Name")).IsFalse();
	}

	[Test]
	public async Task ValueObjectComparable_GeneratesSelfRelationalOperators(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.ValueObject]
				public readonly partial record struct Money
				{
					public decimal Amount { get; }

					private Money(decimal amount)
					{
						Amount = amount;
					}
				}
			}
			""";

		var result = await GenerateAsync(source, cancellationToken);

		var query = result.Generated();
		var money = query.GetRecord("Money", "Testing");
		await Assert.That(HasOperator(query, money, "<", "Money", "Money")).IsTrue();
		await Assert.That(HasOperator(query, money, ">", "Money", "Money")).IsTrue();
		await Assert.That(HasOperator(query, money, "<=", "Money", "Money")).IsTrue();
		await Assert.That(HasOperator(query, money, ">=", "Money", "Money")).IsTrue();
	}

	[Test]
	public async Task ComplexValueObjectGeneration_SupportsRecordClassesWithMultipleProperties(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.ValueObject]
				public sealed partial record UserDetails
				{
					public System.Guid Id { get; }

					public string DisplayName { get; }

					partial void OnValidate(System.Guid id, string displayName)
					{
						if (id == System.Guid.Empty)
							throw new System.ArgumentException("Id must be a valid GUID.", nameof(id));

						if (string.IsNullOrWhiteSpace(displayName))
							throw new System.ArgumentException("DisplayName cannot be null or empty.", nameof(displayName));
					}
				}

				[Purview.EventSourcing.Serialization.ValueObject]
				public sealed partial record UserDetails2(System.Guid Id, string DisplayName)
				{
					partial void OnValidate(System.Guid id, string displayName)
					{
						if (id == System.Guid.Empty)
							throw new System.ArgumentException("Id must be a valid GUID.", nameof(id));

						if (string.IsNullOrWhiteSpace(displayName))
							throw new System.ArgumentException("DisplayName cannot be null or empty.", nameof(displayName));
					}
				}

				public static class UserDetailsHarness
				{
					public static bool UserDetailsValidationThrows()
					{
						try
						{
							_ = UserDetails.Create(System.Guid.Empty, "Display");
							return false;
						}
						catch (System.ArgumentException)
						{
							return true;
						}
					}

					public static bool UserDetails2ValidationThrows()
					{
						try
						{
							_ = UserDetails2.Create(System.Guid.Empty, "Display");
							return false;
						}
						catch (System.ArgumentException)
						{
							return true;
						}
					}

					public static int UserDetailsHashSetCount()
					{
						var id = System.Guid.Parse("11111111-1111-1111-1111-111111111111");
						var values = new System.Collections.Generic.HashSet<UserDetails>
						{
							UserDetails.Create(id, "Alice"),
							UserDetails.Create(id, "Alice"),
							UserDetails.Create(System.Guid.Parse("22222222-2222-2222-2222-222222222222"), "Alice")
						};
						return values.Count;
					}

					public static int UserDetails2HashSetCount()
					{
						var id = System.Guid.Parse("11111111-1111-1111-1111-111111111111");
						var values = new System.Collections.Generic.HashSet<UserDetails2>
						{
							UserDetails2.Create(id, "Alice"),
							UserDetails2.Create(id, "Alice"),
							UserDetails2.Create(System.Guid.Parse("22222222-2222-2222-2222-222222222222"), "Alice")
						};
						return values.Count;
					}

					public static int CompareUserDetails() =>
						UserDetails.Create(System.Guid.Parse("11111111-1111-1111-1111-111111111111"), "Alice")
							.CompareTo(UserDetails.Create(System.Guid.Parse("22222222-2222-2222-2222-222222222222"), "Alice"));

					public static int CompareUserDetails2() =>
						UserDetails2.Create(System.Guid.Parse("11111111-1111-1111-1111-111111111111"), "Alice")
							.CompareTo(UserDetails2.Create(System.Guid.Parse("22222222-2222-2222-2222-222222222222"), "Alice"));
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var harnessType = assembly.GetType("Testing.UserDetailsHarness")!;

		var userDetailsValidationThrows = (bool)
			harnessType.GetMethod("UserDetailsValidationThrows")!.Invoke(null, null)!;
		var userDetails2ValidationThrows = (bool)
			harnessType.GetMethod("UserDetails2ValidationThrows")!.Invoke(null, null)!;
		var userDetailsHashSetCount = (int)harnessType.GetMethod("UserDetailsHashSetCount")!.Invoke(null, null)!;
		var userDetails2HashSetCount = (int)harnessType.GetMethod("UserDetails2HashSetCount")!.Invoke(null, null)!;
		var compareUserDetails = (int)harnessType.GetMethod("CompareUserDetails")!.Invoke(null, null)!;
		var compareUserDetails2 = (int)harnessType.GetMethod("CompareUserDetails2")!.Invoke(null, null)!;

		await Assert.That(userDetailsValidationThrows).IsTrue();
		await Assert.That(userDetails2ValidationThrows).IsTrue();
		await Assert.That(userDetailsHashSetCount).IsEqualTo(2);
		await Assert.That(userDetails2HashSetCount).IsEqualTo(2);
		await Assert.That(compareUserDetails).IsEqualTo(-1);
		await Assert.That(compareUserDetails2).IsEqualTo(-1);
	}

	[Test]
	public async Task ComplexValueObjectGeneration_GeneratesEfConstructorsForAllUserCaptureShapes(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.ValueObject]
				public sealed partial record UserCaptureRecord(UserDetails User, System.DateTimeOffset OccurredAt);

				[Purview.EventSourcing.Serialization.ValueObject]
				public partial record struct UserCaptureRecordStruct(UserDetails User, System.DateTimeOffset OccurredAt);

				[Purview.EventSourcing.Serialization.ValueObject]
				public readonly partial record struct UserCaptureRecordStruct1(UserDetails User, System.DateTimeOffset OccurredAt);

				[Purview.EventSourcing.Serialization.ValueObject]
				public sealed partial record class UserCaptureRecordClass(UserDetails User, System.DateTimeOffset OccurredAt);

				[Purview.EventSourcing.Serialization.ValueObject]
				public sealed partial class UserCaptureClass(UserDetails User, System.DateTimeOffset OccurredAt);

				[Purview.EventSourcing.Serialization.ValueObject]
				public partial struct UserCaptureStruct(UserDetails User, System.DateTimeOffset OccurredAt);

				[Purview.EventSourcing.Serialization.ValueObject]
				public partial class UserDetails
				{
					public System.Guid Id { get; }

					public string DisplayName { get; }
				}

				public static class UserCaptureHarness
				{
					public static object[] CreateAll()
					{
						var user = UserDetails.Create(
							System.Guid.Parse("11111111-1111-1111-1111-111111111111"),
							"Alice"
						);

						return
						[
							UserCaptureRecord.Create(user, System.DateTimeOffset.UnixEpoch),
							UserCaptureRecordStruct.Create(user, System.DateTimeOffset.UnixEpoch),
							UserCaptureRecordStruct1.Create(user, System.DateTimeOffset.UnixEpoch),
							UserCaptureRecordClass.Create(user, System.DateTimeOffset.UnixEpoch),
							UserCaptureClass.Create(),
							UserCaptureStruct.Create()
						];
					}
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);

		var query = result.Generated();
		var recordCtor = query.GetRecord("UserCaptureRecord", "Testing").GetConstructor();
		await Assert.That(recordCtor.Node.Modifiers.ToString()).Contains("private");
		await Assert.That(recordCtor.Node.Initializer?.ToString()).Contains("this(");

		var recordStructCtor = query.GetRecord("UserCaptureRecordStruct", "Testing").GetConstructor();
		await Assert.That(recordStructCtor.Node.Modifiers.ToString()).Contains("public");
		await Assert.That(recordStructCtor.Node.Initializer?.ToString()).Contains("this(");

		var recordStruct1Ctor = query.GetRecord("UserCaptureRecordStruct1", "Testing").GetConstructor();
		await Assert.That(recordStruct1Ctor.Node.Modifiers.ToString()).Contains("public");
		await Assert.That(recordStruct1Ctor.Node.Initializer?.ToString()).Contains("this(");

		var recordClassCtor = query.GetRecord("UserCaptureRecordClass", "Testing").GetConstructor();
		await Assert.That(recordClassCtor.Node.Modifiers.ToString()).Contains("private");
		await Assert.That(recordClassCtor.Node.Initializer?.ToString()).Contains("this(");

		var classCtor = query.GetClass("UserCaptureClass", "Testing").GetConstructor();
		await Assert.That(classCtor.Node.Modifiers.ToString()).Contains("private");
		await Assert.That(classCtor.Node.Initializer?.ToString()).Contains("this(");

		var structCtor = query.GetStruct("UserCaptureStruct", "Testing").GetConstructor();
		await Assert.That(structCtor.Node.Modifiers.ToString()).Contains("public");
		await Assert.That(structCtor.Node.Initializer?.ToString()).Contains("this(");

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var harnessType = assembly.GetType("Testing.UserCaptureHarness")!;
		var created = (object[])harnessType.GetMethod("CreateAll")!.Invoke(null, null)!;

		await Assert.That(created.Length).IsEqualTo(6);
	}

	[Test]
	public async Task ComplexValueObjectGeneration_SupportsNormalizeHook(CancellationToken cancellationToken)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.ValueObject]
				public sealed partial record UserDetails(System.Guid Id, string? DisplayName, bool IsActive = true)
				{
					static partial void OnNormalize(ref System.Guid id, ref string? displayName, ref bool isActive)
					{
						displayName = displayName?.Trim();

						if (!isActive)
							displayName = null;
					}

					partial void OnValidate(System.Guid id, string? displayName, bool isActive)
					{
						if (id == System.Guid.Empty)
							throw new System.ArgumentException("Id must be a valid GUID.", nameof(id));

						if (isActive && string.IsNullOrWhiteSpace(displayName))
							throw new System.ArgumentException("DisplayName cannot be null or empty.", nameof(displayName));
					}
				}

				public static class UserDetailsNormalizeHarness
				{
					public static string ActiveDisplayName()
					{
						var value = UserDetails.Create(System.Guid.Parse("11111111-1111-1111-1111-111111111111"), " Alice ", true);
						return value.DisplayName!;
					}

					public static bool InactiveDisplayNameIsNull()
					{
						var value = UserDetails.Create(System.Guid.Parse("11111111-1111-1111-1111-111111111111"), "Alice", false);
						return value.DisplayName is null;
					}

					public static bool ActiveBlankDisplayNameThrows()
					{
						try
						{
							_ = UserDetails.Create(System.Guid.Parse("11111111-1111-1111-1111-111111111111"), "  ", true);
							return false;
						}
						catch (System.ArgumentException)
						{
							return true;
						}
					}
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();

		var harnessType = assembly.GetType("Testing.UserDetailsNormalizeHarness")!;

		var activeDisplayName = (string)harnessType.GetMethod("ActiveDisplayName")!.Invoke(null, null)!;
		var inactiveDisplayNameIsNull = (bool)harnessType.GetMethod("InactiveDisplayNameIsNull")!.Invoke(null, null)!;
		var activeBlankDisplayNameThrows = (bool)
			harnessType.GetMethod("ActiveBlankDisplayNameThrows")!.Invoke(null, null)!;

		await Assert.That(activeDisplayName).IsEqualTo("Alice");
		await Assert.That(inactiveDisplayNameIsNull).IsTrue();
		await Assert.That(activeBlankDisplayNameThrows).IsTrue();
	}

	[Test]
	public async Task ComplexValueObjectGeneration_RespectsReadOnlyOnValidateForStruct(
		CancellationToken cancellationToken
	)
	{
		const string source = """
			namespace Testing
			{
				[Purview.EventSourcing.Serialization.ValueObject]
				public partial record struct UserDetails(System.Guid Id, string? DisplayName, bool IsActive = true)
				{
					readonly partial void OnValidate(System.Guid id, string? displayName, bool isActive)
					{
						if (id == System.Guid.Empty)
							throw new System.ArgumentException("Id must be a valid GUID.", nameof(id));

						if (isActive && string.IsNullOrWhiteSpace(displayName))
							throw new System.ArgumentException(
								"DisplayName cannot be null or empty when a user is active.",
								nameof(displayName)
							);
					}
				}

				public static class UserDetailsReadOnlyHarness
				{
					public static bool ActiveBlankDisplayNameThrows()
					{
						try
						{
							_ = UserDetails.Create(System.Guid.Parse("11111111-1111-1111-1111-111111111111"), "  ", true);
							return false;
						}
						catch (System.ArgumentException)
						{
							return true;
						}
					}
				}
			}
			""";

		var result = await GenerateAsync(
			source,
			EventSourcingGeneratorTestOptions.Default.Compile(),
			cancellationToken
		);
		var query = result.Generated();
		var onValidate = query
			.GetRecord("UserDetails", "Testing")
			.GetMethod("OnValidate", TypeRefs.Guid, TypeRefs.String, TypeRefs.Bool);
		var modifiers = onValidate.Node.Modifiers.ToString();
		await Assert.That(modifiers).Contains("readonly");
		await Assert.That(modifiers).Contains("partial");

		var assembly = await Assert.That(result.CompilationResult.Assembly).IsNotNull();
		var harnessType = assembly.GetType("Testing.UserDetailsReadOnlyHarness")!;

		var activeBlankDisplayNameThrows = (bool)
			harnessType.GetMethod("ActiveBlankDisplayNameThrows")!.Invoke(null, null)!;

		await Assert.That(activeBlankDisplayNameThrows).IsTrue();
	}

	static bool HasOperator(
		CodeQuery query,
		TypeDeclarationSyntax type,
		string operatorToken,
		params string[] parameterTypeContains
	) => GetOperator(query, type, operatorToken, parameterTypeContains) is not null;

	static OperatorDeclarationSyntax? GetOperator(
		CodeQuery query,
		TypeDeclarationSyntax type,
		string operatorToken,
		params string[] parameterTypeContains
	)
	{
		foreach (var candidate in query.In(type).GetAll<OperatorDeclarationSyntax>())
		{
			if (candidate.Node.OperatorToken.Text != operatorToken)
				continue;

			var parameters = candidate.Node.ParameterList.Parameters;
			if (parameters.Count != parameterTypeContains.Length)
				continue;

			var allMatch = true;
			for (var index = 0; index < parameters.Count; index++)
			{
				if (
					parameters[index].Type?.ToString().Contains(parameterTypeContains[index], StringComparison.Ordinal)
					!= true
				)
				{
					allMatch = false;
					break;
				}
			}

			if (allMatch)
				return candidate;
		}

		return null;
	}
}
