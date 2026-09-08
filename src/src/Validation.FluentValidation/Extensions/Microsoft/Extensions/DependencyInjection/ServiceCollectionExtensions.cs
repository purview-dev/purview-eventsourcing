using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Purview.EventSourcing.Validation;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering FluentValidation aggregate-validator adapters with the dependency
/// injection container.
/// </summary>
/// <remarks>
/// The members of this type are hidden from IntelliSense as the type is only intended to be consumed
/// through the <see langword="static"/> using for the <c>Microsoft.Extensions.DependencyInjection</c> namespace.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers a <see cref="FluentValidationAggregateValidator{TAggregate}"/> adapter
	/// for the specified aggregate type, wrapping the registered
	/// <see cref="IValidator{TAggregate}"/>.
	/// </summary>
	/// <typeparam name="TAggregate">The aggregate type.</typeparam>
	/// <typeparam name="TValidator">The FluentValidation validator implementation.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="lifetime">The lifetime used to register the validator</param>
	/// <returns>The <paramref name="services"/> for fluent chaining.</returns>
	public static IServiceCollection AddFluentValidationAdapter<TAggregate, TValidator>(
		[NotNull] this IServiceCollection services,
		ServiceLifetime lifetime = ServiceLifetime.Singleton
	)
		where TAggregate : Purview.EventSourcing.Aggregates.IAggregate
		where TValidator : class, IValidator<TAggregate>
	{
		services.Add(new(typeof(IValidator<TAggregate>), typeof(TValidator), lifetime));
		services.AddSingleton<IAggregateValidator<TAggregate>, FluentValidationAggregateValidator<TAggregate>>();

		return services;
	}

	/// <summary>
	/// Registers a <see cref="FluentValidationAggregateValidator{TAggregate}"/> adapter
	/// using a factory that resolves <see cref="IValidator{TAggregate}"/> from the container.
	/// Use this when validators are already registered (e.g. via <c>AddValidatorsFromAssembly</c>).
	/// </summary>
	/// <typeparam name="TAggregate">The aggregate type.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="lifetime">The lifetime used to register the validator</param>
	/// <returns>The <paramref name="services"/> for fluent chaining.</returns>
	public static IServiceCollection AddFluentValidationAdapter<TAggregate>(
		[NotNull] this IServiceCollection services,
		ServiceLifetime lifetime = ServiceLifetime.Singleton
	)
		where TAggregate : Purview.EventSourcing.Aggregates.IAggregate
	{
		services.Add(
			new(
				typeof(IValidator<TAggregate>),
				sp =>
				{
					var validator = sp.GetService<IValidator<TAggregate>>();
					return validator is null ? null! : new FluentValidationAggregateValidator<TAggregate>(validator);
				},
				lifetime
			)
		);

		return services;
	}
}
