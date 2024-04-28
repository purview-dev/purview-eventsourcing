using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Purview.EventSourcing;

[EditorBrowsable(EditorBrowsableState.Never)]
static class ExtensionHelpers
{
	[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
	public static string ToLowerSafe(this string value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

		return value.ToLowerInvariant();
	}
}
