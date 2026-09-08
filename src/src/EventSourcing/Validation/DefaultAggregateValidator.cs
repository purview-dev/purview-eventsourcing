using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Validation;

/// <summary>
/// A default validator for <see cref="IAggregate"/>'s based on
/// standard data annotations.
/// </summary>
public sealed class DefaultAggregateValidator<TAggregate> : IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	/// <summary>
	/// A statically cached instance based on the use of standard data annotations.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
	public static IAggregateValidator<TAggregate> Instance { get; } = new DefaultAggregateValidator<TAggregate>();

	/// <summary>
	/// Validates the aggregate using standard data annotations.
	/// </summary>
	/// <param name="aggregate">The aggregate to validate.</param>
	/// <returns>A <see cref="ValidationResult"/> describing any annotation failures.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is null.</exception>
	public ValidationResult Validate(TAggregate aggregate)
	{
		ArgumentNullException.ThrowIfNull(aggregate);

		var failures = ValidateWithAnnotations(aggregate);
		return new ValidationResult(failures);
	}

	/// <summary>
	/// Validates the aggregate using standard data annotations.
	/// </summary>
	/// <param name="aggregate">The aggregate to validate.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A task whose result is a <see cref="ValidationResult"/> describing any annotation failures.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is null.</exception>
	public Task<ValidationResult> ValidateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(aggregate);

		var failures = ValidateWithAnnotations(aggregate);
		return Task.FromResult(new ValidationResult(failures));
	}

	static IEnumerable<ValidationFailure> ValidateWithAnnotations(TAggregate aggregate)
	{
		System.ComponentModel.DataAnnotations.ValidationContext daContext = new(aggregate);
		List<System.ComponentModel.DataAnnotations.ValidationResult> failures = [];

		if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(aggregate, daContext, failures, true))
		{
			foreach (var failure in failures)
			{
				foreach (var memberName in failure.MemberNames)
					yield return new ValidationFailure(
						memberName,
						failure.ErrorMessage ?? "Validation failed (no error provided)"
					);
			}
		}
	}
}
