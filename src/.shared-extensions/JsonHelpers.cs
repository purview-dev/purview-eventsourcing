namespace Purview.EventSourcing;

static class JsonHelpers
{
	static public readonly Newtonsoft.Json.JsonSerializerSettings JsonSerializerSettings = new()
	{
		TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None,
		ContractResolver = new Newtonsoft.Json.Serialization.PrivateSetterContractResolver(),
		NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
		Formatting = Newtonsoft.Json.Formatting.None,
		Converters = [new Newtonsoft.Json.Converters.StringValuesConverter()]
	};
}
