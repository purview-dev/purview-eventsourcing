using Microsoft.CodeAnalysis;

namespace Purview.EventSourcing.SourceGenerator;

[Generator]
public sealed partial class EventSourcingSourceGenerator : IIncrementalGenerator, ILogSupport
{
	IGenerationLogger? _logger;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Register all of the shared attributes we need.
		context.RegisterPostInitializationOutput(ctx =>
		{
			_logger?.Debug("--- Adding templates.");

			foreach (var template in Constants.GetAllTemplates())
			{
				_logger?.Debug($"Adding {template.Name} as {template.GetGeneratedFilename()}.");

				ctx.AddSource(template.GetGeneratedFilename(), template.TemplateData);
			}

			_logger?.Debug("--- Finished adding templates.");
		});

		RegisterIAggregate(context, _logger);
		RegisterEventProperty(context, _logger);
	}

	void ILogSupport.SetLogOutput(Action<string, OutputType> action)
	{
		if (action != null)
		{
			_logger = new Logger(action);
		}
	}
}
