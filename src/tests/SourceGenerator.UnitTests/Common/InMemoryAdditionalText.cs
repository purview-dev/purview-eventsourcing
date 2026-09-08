using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Purview.EventSourcing.SourceGenerator.Common;

/// <summary>
/// An in-memory <see cref="AdditionalText"/> used to supply a baseline event-contract manifest
/// to generator tests without touching the filesystem. Equality is value-based (path and content)
/// so incremental runs can observe content changes.
/// </summary>
sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
{
	public override string Path { get; } = path;

	readonly string _text = text;

	public override SourceText GetText(CancellationToken cancellationToken = default) =>
		SourceText.From(_text, Encoding.UTF8);

	public override int GetHashCode() => HashCode.Combine(StringComparer.Ordinal.GetHashCode(Path), _text);

	public override bool Equals(object? obj) =>
		obj is InMemoryAdditionalText other
		&& StringComparer.Ordinal.Equals(Path, other.Path)
		&& StringComparer.Ordinal.Equals(_text, other._text);
}
