namespace Purview.EventSourcing;

/// <summary>
/// Defines a class that should have the <see cref="Purview.EventSourcing.Interfaces.Aggregates.IAggregate"/> generated.
/// By default, <see cref="GenerateImplicitly"/> is true.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[System.Diagnostics.Conditional("PURVIEW_EVENTSOURCE_EMBED_ATTRIBUTES")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed public class GenerateAggregateAttribute : System.Attribute {
	/// <summary>
	/// Creates a new instance of the <see cref="GenerateAggregateAttribute"/> class.
	/// </summary>
	public GenerateAggregateAttribute() {
	}

	/// <summary>
	/// Creates a new instance of the <see cref="GenerateAggregateAttribute"/> class.
	/// </summary>
	/// <param name="generateImplicitly">Sets the <see cref="GenerateImplicitly"/> property.</param>
	public GenerateAggregateAttribute(bool generateImplicitly) {
		GenerateImplicitly = generateImplicitly;
	}

	/// <summary>
	/// Indicates if the <see cref="Purview.EventSourcing.Interfaces.Aggregates.IAggregate"/> should be generated implicitly.
	/// True to generate the IAggregate implicitly; otherwise, false to generate it explicitly/ publicly. The default is true.
	/// </summary>
	public bool GenerateImplicitly { get; set; } = true;
}
