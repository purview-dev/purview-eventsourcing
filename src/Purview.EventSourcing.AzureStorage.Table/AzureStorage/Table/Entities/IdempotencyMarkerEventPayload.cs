namespace Purview.EventSourcing.AzureStorage.Table.Entities;

public sealed class IdempotencyMarkerEventPayload
{
	public int[] EventIds { get; set; } = default!;
}
