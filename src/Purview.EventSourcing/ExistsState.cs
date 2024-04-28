using System.Diagnostics.CodeAnalysis;

namespace Purview.EventSourcing;

/// <summary>
/// The result of an exists operations on an aggregate. Is implicitly comparable to a <see cref="bool" />.
/// </summary>
/// <remarks>
/// <para>
/// When converting to a <see cref="bool"/>, when the <see cref="Status" /> is
/// <see cref="ExistsStatus.Exists"/> or <see cref="ExistsStatus.ExistsInDeletedState"/>, true is returned.
/// </para>
/// <para>Otherwise, false is returned.</para>
/// </remarks>
[System.Diagnostics.DebuggerStepThrough]
public sealed record ExistsState : IEquatable<ExistsState>
{
	/// <summary>
	/// Converts this instance of a <see cref="ExistsState"/> to a <see cref="bool"/>.
	/// <param name="state">The value to convert.</param>
	/// <returns>Returns true if the <see cref="Status"/> equals <see cref="ExistsStatus.Exists"/>
	/// </summary>
	public static implicit operator bool([NotNull] ExistsState state)
		=> state.Status is
			ExistsStatus.Exists
			or ExistsStatus.ExistsInDeletedState;

	/// <summary>
	/// Represents a value that indicates the aggregate does not exist.
	/// </summary>
	public static ExistsState DoesNotExists => new()
	{
		Status = ExistsStatus.DoesNotExist
	};

	/// <summary>
	/// Represents a value that indicates the aggregate does exist.
	/// </summary>
	public static ExistsState Exists => new()
	{
		Status = ExistsStatus.Exists
	};

	/// <summary>
	/// Represents a value that indicates the aggregate exists, but is in a deleted state.
	/// </summary>
	public static ExistsState ExistsInDeletedState => new()
	{
		Status = ExistsStatus.ExistsInDeletedState
	};

	/// <summary>
	/// If <see cref="Status"/> is <see cref="ExistsStatus.Exists"/>
	/// or <see cref="ExistsStatus.ExistsInDeletedState"/>, then returns true.
	/// Otherwise, returns false.
	/// </summary>
	public bool DoesExist => this;

	/// <summary>
	/// Gets the <see cref="ExistsStatus"/> of the aggregate exists result.
	/// </summary>
	public ExistsStatus Status { get; set; }

	/// <summary>
	/// Gets the version of the aggregate if it exists, regardless of it's
	/// deleted state.
	/// </summary>
	public int? Version { get; set; }

	/// <summary>
	/// Determines if this instance is equals to the <paramref name="other"/> instance.
	/// </summary>
	/// <param name="other">The instance to compare.</param>
	/// <returns>True if the instances are the same, otherwise false.</returns>
	public bool Equals(ExistsState? other)
	{
		if (other == null)
			return false;

		return Status == other.Status &&
			Version == other.Version;
	}

	/// <summary>
	/// Generates a hash-code for this instance.
	/// </summary>
	/// <returns>A hash of this instance.</returns>
	public override int GetHashCode()
	{
		HashCode hashCode = new();

		hashCode.Add(Status);
		if (Version.HasValue)
			hashCode.Add(Version.Value);

		return hashCode.ToHashCode();
	}
}
