using FluentValidation;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Validation.FluentValidation;

public sealed class FluentValidationAggregateValidatorTests
{
	[Test]
	public async Task ValidateAsync_UsesAsyncRules(CancellationToken cancellationToken)
	{
		var asyncRuleInvoked = false;
		var aggregate = new TestAggregate { Name = "invalid" };
		var validator = new InlineValidator<TestAggregate>();
		validator
			.RuleFor(m => m.Name)
			.MustAsync(
				(_, _) =>
				{
					asyncRuleInvoked = true;
					return Task.FromResult(true);
				}
			);

		var adapter = new FluentValidationAggregateValidator<TestAggregate>(validator);

		var result = await adapter.ValidateAsync(aggregate, cancellationToken);

		await Assert.That(asyncRuleInvoked).IsTrue();
		await Assert.That(result.IsValid).IsTrue();
	}

	[Test]
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1849:Call async methods when in an async method"
	)]
	public async Task Validate_WhenFluentValidationFails_MapsToCoreValidationResult()
	{
		var aggregate = new TestAggregate { Name = "" };
		var validator = new InlineValidator<TestAggregate>();
		validator.RuleFor(m => m.Name).NotEmpty();

		var adapter = new FluentValidationAggregateValidator<TestAggregate>(validator);

		var result = adapter.Validate(aggregate);

		await Assert.That(result.IsValid).IsFalse();
		await Assert.That(result.Failures).Count().IsEqualTo(1);
		await Assert.That(result.Failures[0].PropertyName).IsEqualTo("Name");
	}

	sealed class TestAggregate : AggregateBase
	{
		public string Name { get; set; } = string.Empty;

		protected override void RegisterEvents() { }
	}
}
