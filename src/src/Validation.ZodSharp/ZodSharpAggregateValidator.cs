using Purview.EventSourcing.Aggregates;
using ZodSharp.Core;

namespace Purview.EventSourcing.Validation.ZodSharp;

/// <summary>
/// Adapts an <see cref="IZodSchemaValidator{TAggregate}"/> to the
/// <see cref="IAggregateValidator{TAggregate}"/> interface, mapping ZodSharp validation results to
/// <see cref="ValidationResult"/>.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type to validate.</typeparam>
/// <param name="validator">The <see cref="IZodSchemaValidator{TAggregate}"/> to adapt.</param>
public sealed class ZodSharpAggregateValidator<TAggregate>(IZodSchemaValidator<TAggregate> validator)
	: IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	///<inheritdoc/>
	public ValidationResult Validate(TAggregate aggregate) => Convert(validator.Validate(aggregate));

	///<inheritdoc/>
	public async Task<ValidationResult> ValidateAsync(
		TAggregate aggregate,
		CancellationToken cancellationToken = default
	)
	{
		var result = await validator.ValidateAsync(aggregate, cancellationToken);

		return Convert(result);
	}

	static ValidationResult Convert(ValidationResult<TAggregate> validationResult) =>
		validationResult.IsSuccess
			? ValidationResult.Success
			: new ValidationResult(
				validationResult.Errors.Select(e => new ValidationFailure(string.Join('.', e.Path), e.Message))
			);
}
