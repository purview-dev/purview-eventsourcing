using FluentValidation;
using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Validation.FluentValidation;

/// <summary>
/// Adapts a <see cref="IValidator{TAggregate}"/> to the
/// <see cref="IAggregateValidator{TAggregate}"/> interface, mapping FluentValidation
/// results to <see cref="ValidationResult"/>.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type to validate.</typeparam>
/// <param name="validator">The <see cref="IValidator{TAggregate}"/> to adapt.</param>
public sealed class FluentValidationAggregateValidator<TAggregate>(IValidator<TAggregate> validator)
	: IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	readonly IValidator<TAggregate> _validator = validator;

	///<inheritdoc/>
	public ValidationResult Validate(TAggregate aggregate) => Map(_validator.Validate(aggregate));

	///<inheritdoc/>
	public async Task<ValidationResult> ValidateAsync(
		TAggregate aggregate,
		CancellationToken cancellationToken = default
	)
	{
		var result = await _validator.ValidateAsync(aggregate, cancellationToken);
		return Map(result);
	}

	static ValidationResult Map(global::FluentValidation.Results.ValidationResult result)
	{
		if (result.IsValid)
			return ValidationResult.Success;

		var failures = result.Errors.Select(e => new ValidationFailure(e.PropertyName, e.ErrorMessage));
		return new(failures);
	}
}
