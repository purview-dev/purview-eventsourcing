namespace Purview.EventSourcing.AzureStorage.Table.Entities;

public sealed class IdempotencyMarkerEventPayload
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "This is a DTO.")]
	public int[] EventIds { get; set; } = default!;
}
