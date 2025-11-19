namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za ažuriranje zavisnog troška
/// </summary>
public record UpdateDocumentCostDto(
    int PartnerId,
    string DocumentTypeCode,
    decimal AmountNet,
    decimal AmountVat,
    DateTime DueDate,
    string? Description);
