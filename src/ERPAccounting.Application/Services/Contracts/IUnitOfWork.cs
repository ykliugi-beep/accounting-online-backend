using System.Threading;

namespace ERPAccounting.Application.Services.Contracts;

/// <summary>
/// Defines a unit-of-work contract that coordinates repository operations within a single persistence context.
/// </summary>
public interface IUnitOfWork
{
    IDocumentLineItemRepository DocumentLineItems { get; }

    IDocumentRepository Documents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
