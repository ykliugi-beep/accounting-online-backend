namespace ERPAccounting.Domain.Enums;

/// <summary>
/// Identifiers for the most common ERP document categories.
/// Maps to <c>IDVrstaDokumenta</c> codes (UR, OD, etc.).
/// </summary>
public enum DocumentType
{
    Unknown = 0,
    IncomingInvoice = 1,
    OutgoingInvoice = 2,
    InternalTransfer = 3,
    AdvanceInvoice = 4,
    InventoryDocument = 5
}
