using System;

namespace ERPAccounting.Domain.Interfaces;

/// <summary>
/// Represents an entity that tracks audit metadata.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    int? CreatedBy { get; }
    int? UpdatedBy { get; }
}
