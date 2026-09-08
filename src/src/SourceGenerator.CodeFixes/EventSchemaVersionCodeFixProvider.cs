using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Purview.EventSourcing.SourceGenerator.Common;

namespace Purview.EventSourcing.SourceGenerator.CodeFixes;

/// <summary>
/// Corrects event schema-version declarations:
/// <list type="bullet">
///   <item><c>EVENTSTORE021</c> resets a non-positive version to 1, the framework default.</item>
///   <item><c>EVENTSTORE022</c> moves a duplicate explicit version to the next unused version on the
///   aggregate, which is deterministic and safe.</item>
/// </list>
/// A fix is only offered when the version argument is explicit and can be located in source.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EventSchemaVersionCodeFixProvider)), Shared]
public sealed class EventSchemaVersionCodeFixProvider : CodeFixProvider
{
	const string EquivalenceKey = "SetEventSchemaVersion";

	/// <inheritdoc/>
	public override ImmutableArray<string> FixableDiagnosticIds =>
		[
			DiagnosticLibrary.EventSchemaVersionMustBePositive.Id,
			DiagnosticLibrary.DuplicateEventSchemaVersionOnAggregate.Id,
		];

	/// <inheritdoc/>
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
			return;

		var semanticModel = await context
			.Document.GetSemanticModelAsync(context.CancellationToken)
			.ConfigureAwait(false);

		foreach (var diagnostic in context.Diagnostics)
		{
			var method = FindMethod(root, diagnostic.Location.SourceSpan.Start);
			if (method is null)
				continue;

			var versionArgument = FindVersionArgument(method);
			if (versionArgument is null)
				continue;

			int newVersion;
			if (diagnostic.Id == DiagnosticLibrary.EventSchemaVersionMustBePositive.Id)
			{
				newVersion = 1;
			}
			else if (diagnostic.Id == DiagnosticLibrary.DuplicateEventSchemaVersionOnAggregate.Id)
			{
				var maxVersion = FindMaxUsedVersion(method, semanticModel, context.CancellationToken);
				newVersion = maxVersion + 1;
			}
			else
			{
				continue;
			}

			context.RegisterCodeFix(
				CodeAction.Create(
					title: $"Set schema version to {newVersion.ToString(CultureInfo.InvariantCulture)}",
					createChangedDocument: cancellationToken =>
						SetVersionAsync(context.Document, versionArgument, newVersion, cancellationToken),
					equivalenceKey: EquivalenceKey
				),
				diagnostic
			);
		}
	}

	static MethodDeclarationSyntax? FindMethod(SyntaxNode root, int position)
	{
		var token = root.FindToken(position);
		return token.Parent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
	}

	static AttributeSyntax? FindEventAttribute(MethodDeclarationSyntax method)
	{
		foreach (var attributeList in method.AttributeLists)
		{
			foreach (var attribute in attributeList.Attributes)
			{
				var name = attribute.Name.ToString();
				if (name.EndsWith("Event", StringComparison.Ordinal))
					return attribute;
			}
		}

		return null;
	}

	static AttributeArgumentSyntax? FindVersionArgument(MethodDeclarationSyntax method)
	{
		var attribute = FindEventAttribute(method);
		if (attribute?.ArgumentList is null)
			return null;

		var arguments = attribute.ArgumentList.Arguments;

		foreach (var argument in arguments)
		{
			if (
				argument.NameEquals is not null
				&& StringComparer.Ordinal.Equals(argument.NameEquals.Name.Identifier.ValueText, "Version")
			)
				return argument;
		}

		// Positional arguments: [Event(N)] uses index 0; [CollectionEvent(propertyName, N)] uses index 1.
		var isCollectionEvent = attribute.Name.ToString().EndsWith("CollectionEvent", StringComparison.Ordinal);
		var versionIndex = isCollectionEvent ? 1 : 0;
		var positionalIndex = 0;
		foreach (var argument in arguments)
		{
			if (argument.NameEquals is null)
			{
				if (positionalIndex == versionIndex)
					return argument;
				positionalIndex++;
			}
		}

		return null;
	}

	static int FindMaxUsedVersion(
		MethodDeclarationSyntax method,
		SemanticModel? semanticModel,
		CancellationToken cancellationToken
	)
	{
		var containingType = method.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		if (containingType is null)
			return 0;

		var max = 0;
		foreach (var candidate in containingType.DescendantNodes().OfType<MethodDeclarationSyntax>())
		{
			var attribute = FindEventAttribute(candidate);
			if (attribute is null)
				continue;

			var argument = FindVersionArgument(candidate);
			if (argument is null || semanticModel is null)
				continue;

			var constant = semanticModel.GetConstantValue(argument.Expression, cancellationToken);
			if (constant.HasValue && constant.Value is int version)
				max = Math.Max(max, version);
		}

		return max;
	}

	static async Task<Document> SetVersionAsync(
		Document document,
		AttributeArgumentSyntax versionArgument,
		int newVersion,
		CancellationToken cancellationToken
	)
	{
		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
		var updated = versionArgument.WithExpression(
			SyntaxFactory.ParseExpression(newVersion.ToString(CultureInfo.InvariantCulture))
		);
		editor.ReplaceNode(versionArgument, updated);
		return editor.GetChangedDocument();
	}
}
