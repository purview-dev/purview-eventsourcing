using Purview.EventSourcing.Aggregates;
using ZodSharp.Core;

namespace Purview.EventSourcing.Validation.ZodSharp;

public sealed class ZodSharpAggregateValidatorTests
{
	[Test]
	public async Task ValidateAsync_UsesAsyncRules(CancellationToken cancellationToken)
	{
		TestAggregate aggregate = new() { Name = "valid" };
		var validator = IZodSchemaValidator<TestAggregate>.Mock();
		validator.ValidateAsync(Any(), Any()).Returns(ValidationResult<TestAggregate>.Success(aggregate));

		ZodSharpAggregateValidator<TestAggregate> adapter = new(validator);

		await adapter.ValidateAsync(aggregate, cancellationToken);

		validator.ValidateAsync(AnyArgs()).WasCalled(Times.Once);
	}

	[Test]
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1849:Call async methods when in an async method"
	)]
	public void Validate_UsesNonAsyncRules()
	{
		TestAggregate aggregate = new() { Name = "valid" };
		var validator = IZodSchemaValidator<TestAggregate>.Mock();
		validator.Validate(Any()).Returns(ValidationResult<TestAggregate>.Success(aggregate));

		ZodSharpAggregateValidator<TestAggregate> adapter = new(validator);

		adapter.Validate(aggregate);

		validator.Validate(Any()).WasCalled(Times.Once);
	}

	[Test]
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1849:Call async methods when in an async method"
	)]
	public async Task Validate_WhenValidationFails_MapsToCoreValidationResult()
	{
		TestAggregate aggregate = new() { Name = "" };
		var validator = IZodSchemaValidator<TestAggregate>.Mock();
		validator
			.Validate(Any())
			.Returns(
				ValidationResult<TestAggregate>.Failure(
					ValidationError.Create("any", "error", [nameof(TestAggregate.Name)])
				)
			);

		ZodSharpAggregateValidator<TestAggregate> adapter = new(validator);

		var result = adapter.Validate(aggregate);

		await Assert.That(result.IsValid).IsFalse();
		await Assert.That(result.Failures).Count().IsEqualTo(1);
		await Assert.That(result.Failures[0].PropertyName).IsEqualTo(nameof(TestAggregate.Name));
	}

	[Test]
	public async Task ValidateAsync_WhenValidationFails_MapsToCoreValidationResult()
	{
		TestAggregate aggregate = new() { Name = "" };
		var validator = IZodSchemaValidator<TestAggregate>.Mock();
		validator
			.ValidateAsync(AnyArgs())
			.Returns(
				ValidationResult<TestAggregate>.Failure(
					ValidationError.Create("any", "error", [nameof(TestAggregate.Name)])
				)
			);

		ZodSharpAggregateValidator<TestAggregate> adapter = new(validator);

		var result = await adapter.ValidateAsync(aggregate);

		await Assert.That(result.IsValid).IsFalse();
		await Assert.That(result.Failures).Count().IsEqualTo(1);
		await Assert.That(result.Failures[0].PropertyName).IsEqualTo(nameof(TestAggregate.Name));
	}

	sealed class TestAggregate : AggregateBase
	{
		public string Name { get; set; } = string.Empty;

		protected override void RegisterEvents() { }
	}
}
