using System.Text;
using Microsoft.CodeAnalysis;
using Purview.EventSourcing.SourceGenerator.Records;
using Purview.EventSourcing.SourceGenerator.Templates;

namespace Purview.EventSourcing.SourceGenerator.Emitters;

static partial class IAggregateImplementationEmitter
{
	public static void GenerateImplementation(IAggregateGenerationTarget target,
		SourceProductionContext context,
		IGenerationLogger? logger)
	{
		logger?.Debug($"Generating IAggregate implementation for: {target.FullyQualifiedName}.");

		StringBuilder builder = new();

		if (!target.IsPartialClass)
		{
			EventSourcingDiagnostics.Report(context.ReportDiagnostic, EventSourcingDiagnostics.TopLevelClass.ClassIsNotPartial, target.ClassLocation);
			return;
		}

		if (!target.IAggregateInterfaceIsPresent)
		{
			EventSourcingDiagnostics.Report(context.ReportDiagnostic, EventSourcingDiagnostics.TopLevelClass.DoesNotImplementIAggregate, target.ClassLocation);
			return;
		}

		foreach (var generatedApplyMethod in target.GeneratedApplyMethods)
		{
			if (generatedApplyMethod.GenerateMethod)
			{
				continue;
			}

			if (generatedApplyMethod.PreDefinedMethod!.IsPublic)
			{
				EventSourcingDiagnostics.Report(context.ReportDiagnostic, EventSourcingDiagnostics.Methods.MethodIsPublic, generatedApplyMethod.PreDefinedMethod.MethodLocation);
			}

			if (!generatedApplyMethod.PreDefinedMethod.IsVoid)
			{
				EventSourcingDiagnostics.Report(context.ReportDiagnostic, EventSourcingDiagnostics.Methods.MethodHasReturnValue, generatedApplyMethod.PreDefinedMethod.MethodLocation);
			}
		}

		foreach (var predefinedMethod in target.PredefinedApplyMethods)
		{
			if (predefinedMethod.IsPublic)
			{
				EventSourcingDiagnostics.Report(context.ReportDiagnostic, EventSourcingDiagnostics.Methods.MethodIsPublic, predefinedMethod.MethodLocation);
			}

			if (!predefinedMethod.IsVoid)
			{
				EventSourcingDiagnostics.Report(context.ReportDiagnostic, EventSourcingDiagnostics.Methods.MethodHasReturnValue, predefinedMethod.MethodLocation);
			}
		}

		var indent = EmitNamespaceStart(target, builder, context);
		indent = EmitClassStart(target, builder, indent, context);

		indent = EmitFields(target, builder, indent, context);
		indent = EmitProperties(target, builder, indent, context, logger);
		indent = EmitMethods(target, builder, indent, context);

		indent = EmitClassEnd(builder, indent, context);
		EmitNamespaceEnd(target, builder, indent, context);

		var sourceText = EmbeddedResources.Instance.AddHeader(builder.ToString());
		var hintName = $"{target.FullyQualifiedName}.Core.g.cs";

		context.AddSource(hintName, Microsoft.CodeAnalysis.Text.SourceText.From(sourceText, Encoding.UTF8));
	}

	static string PublicOrImplicitModifier(IAggregateGenerationTarget target, string typeInfo)
	{
		return (target.GenerateIAggregateImplicitly
			? ""
			: "public ") + typeInfo;
	}

	static string PublicOrImplicitIAggregate(IAggregateGenerationTarget target)
	{
		return target.GenerateIAggregateImplicitly
			? Constants.EventStore.IAggregate + "."
			: "";
	}
}
