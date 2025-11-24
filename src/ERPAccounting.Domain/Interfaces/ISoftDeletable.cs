namespace ERPAccounting.Domain.Interfaces;

/// <summary>
/// Marker interface for entities that support soft deletion via the <c>IsDeleted</c> flag.
/// </summary>
public interface ISoftDeletable : IEntity
{
    bool IsDeleted { get; set; }
}
