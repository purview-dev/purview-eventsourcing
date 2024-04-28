using System.Runtime.CompilerServices;

namespace Purview.EventSourcing.SourceGenerator;

public static class ModuleInitialiser
{
	[ModuleInitializer]
	public static void Init()
	{
		VerifySourceGenerators.Initialize();
	}
}
