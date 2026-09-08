using Purview.EventSourcing.Admin.Abstractions.Models;

namespace Purview.EventSourcing.Admin.Abstractions.Services;

/// <summary>
/// Records privileged Admin portal operations. Applications may replace the default in-memory
/// implementation with a durable audit store.
/// </summary>
public interface IAdminAuditLogger
{
	/// <summary>Writes one audit entry.</summary>
	Task LogAsync(AdminAuditEntry entry, CancellationToken cancellationToken);
}
