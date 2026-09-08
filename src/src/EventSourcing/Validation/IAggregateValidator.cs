using Purview.EventSourcing.Aggregates;

namespace Purview.EventSourcing.Validation;

/// <summary>
/// Validates an aggregate before it is persisted.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type to validate.</typeparam>
public interface IAggregateValidator<TAggregate>
	where TAggregate : IAggregate
{
	/// <summary>
	/// Validates the aggregate and returns any validation failures.
	/// </summary>
	/// <param name="aggregate">The aggregate to validate.</param>
	/// <returns>A <see cref="ValidationResult"/> describing any failures.</returns>
	ValidationResult Validate(TAggregate aggregate);

	/// <summary>
	/// Asynchronously validates the aggregate and returns any validation failures.
	/// </summary>
	/// <param name="aggregate">The aggregate to validate.</param>
	/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
	/// <returns>A task whose result is a <see cref="ValidationResult"/> describing any failures.</returns>
	Task<ValidationResult> ValidateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}
