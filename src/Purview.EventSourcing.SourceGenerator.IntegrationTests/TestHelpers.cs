using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Purview.EventSourcing.SourceGenerator.Templates;

namespace Purview.EventSourcing.SourceGenerator;

static partial class TestHelpers
{
	static readonly Assembly _ownerAssembly = typeof(TestHelpers).Assembly;
	static readonly string _namespaceRoot = typeof(TestHelpers).Namespace!;

	public const string DefaultUsingSet = @$"
using System;

";

	public static string Wrap(this string value, char c = '"')
		=> c + value + c;

	public static string LoadEmbeddedResource(string folder, string resourceName)
	{
		resourceName = $"{_namespaceRoot}.Resources.{folder}.{resourceName}";

		var resourceStream = _ownerAssembly.GetManifestResourceStream(resourceName);
		if (resourceStream is null)
		{
			var existingResources = _ownerAssembly.GetManifestResourceNames();
			throw new ArgumentException($"Could not find embedded resource {resourceName}. Available resource names: {string.Join(", ", existingResources)}");
		}

		using StreamReader reader = new(resourceStream, Encoding.UTF8);

		return reader.ReadToEnd();
	}

	public static bool IsModifierPresent(MemberDeclarationSyntax member, SyntaxKind modifier)
		=> member.Modifiers.Any(m => m.IsKind(modifier));

	public static List<string> GetCasePermutations(string input)
	{
		List<string> result = [];

		if (string.IsNullOrWhiteSpace(input))
		{
			result.Add(input);
			return result;
		}

		char currentChar = input[0];
		string remainder = input.Substring(1);
		List<string> remainderPermutations = GetCasePermutations(remainder);

		if (char.IsLetter(currentChar))
		{
			foreach (string s in remainderPermutations)
			{
				result.Add(char.ToLower(currentChar, System.Globalization.CultureInfo.InvariantCulture) + s);
				result.Add(char.ToUpper(currentChar, System.Globalization.CultureInfo.InvariantCulture) + s);
			}
		}
		else
		{
			foreach (string s in remainderPermutations)
			{
				result.Add(currentChar + s);
			}
		}

		return result;
	}

	public static async Task Verify(GenerationResult generationResult,
		Action<SettingsTask>? config = null,
		bool validateNonEmptyDiagnostics = false,
		bool whenValidatingDiagnosticsIgnoreNonErrors = false,
		bool validationCompilation = true,
		bool autoVerifyTemplates = true)
	{

		var verifierTask = Verifier
			.Verify(generationResult.Result)
			.UseDirectory("Snapshots")
			.DisableRequireUniquePrefix()
			.DisableDateCounting()
			.UniqueForTargetFrameworkAndVersion(typeof(TestHelpers).Assembly)
			.ScrubInlineDateTimeOffsets("yyyy-MM-dd HH:mm:ss zzzz") // 2024-22-02 14:43:22 +00:00
			.AutoVerify(file =>
			{
				if (autoVerifyTemplates)
				{
					foreach (TemplateInfo template in Constants.GetAllTemplates())
					{
						string potentialName = $"#{template.Name}.g.";

						if (file.IndexOf(potentialName, StringComparison.Ordinal) > -1)
						{
							return true;
						}
					}
				}

				return false;
			})
		;

		config?.Invoke(verifierTask);

		//verifierTask = verifierTask.AutoVerify();

		await verifierTask;

		var diag = generationResult.Diagnostics.AsEnumerable();
		if (whenValidatingDiagnosticsIgnoreNonErrors)
		{
			diag = diag.Where(m => m.Severity == DiagnosticSeverity.Error);
		}

		if (validateNonEmptyDiagnostics)
		{
			diag.Should().NotBeEmpty();
		}
		else
		{
			diag.Should().BeEmpty();
		}

		if (!validationCompilation)
		{
			return;
		}

#if NET7_0_OR_GREATER
		await
#endif
			using MemoryStream ms = new();

		EmitResult result = generationResult.Compilation.Emit(ms);

		if (!result.Success)
		{
			result
				.Diagnostics
				.Where(m => !m.Id.StartsWith("ESS", StringComparison.Ordinal))
				.Should()
				.BeEmpty(string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString() + Environment.NewLine + "-----------------------------------------------------")));
		}
	}
}
