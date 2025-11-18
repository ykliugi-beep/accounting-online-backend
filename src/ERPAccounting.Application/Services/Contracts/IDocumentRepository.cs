using System.Threading;

namespace ERPAccounting.Application.Services.Contracts;

/// <summary>
/// Repository abstraction for high-level document data access used by application services.
/// </summary>
public interface IDocumentRepository
{
    Task<bool> ExistsAsync(int documentId, CancellationToken cancellationToken = default);
}
