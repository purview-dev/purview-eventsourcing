using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Purview.EventSourcing.AzureStorage.Table;

static class TableEventStoreConstants
{
	public const string StreamVersionRowKey = "version";
	public const string IdempotencyCheckRowKeyPrefix = "i_";
	public const string SnapshotFilename = "snapshot.json";

	public static readonly JsonSerializerSettings JsonSerializerSettings = new()
	{
		TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
		TypeNameHandling = TypeNameHandling.None,
		NullValueHandling = NullValueHandling.Ignore,
		ContractResolver = new PrivateSetterContractResolver(),
		Converters = [
			new Newtonsoft.Json.Converters.StringValuesConverter()
		]
	};
}
