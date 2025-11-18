namespace ERPAccounting.Domain.Enums;

/// <summary>
/// Type of business operation performed by the document.
/// </summary>
public enum OperationType
{
    Undefined = 0,
    Purchase = 1,
    Sale = 2,
    Transfer = 3,
    Service = 4,
    InventoryAdjustment = 5
}
