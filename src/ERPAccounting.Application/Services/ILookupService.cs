using ERPAccounting.Application.DTOs;

namespace ERPAccounting.Application.Services;

/// <summary>
/// Provides a controller-facing abstraction for all lookup related operations.
/// Coordinates calls to the stored-procedure service while keeping
/// defaulting/validation logic inside the application layer.
/// </summary>
public interface ILookupService
{
    Task<List<PartnerComboDto>> GetPartnerComboAsync();
    Task<List<OrgUnitComboDto>> GetOrgUnitsComboAsync(string? docTypeId = null);
    Task<List<TaxationMethodComboDto>> GetTaxationMethodsComboAsync();
    Task<List<ReferentComboDto>> GetReferentsComboAsync();
    Task<List<DocumentNDComboDto>> GetDocumentNDComboAsync();
    Task<List<TaxRateComboDto>> GetTaxRatesComboAsync();
    Task<List<ArticleComboDto>> GetArticlesComboAsync();
    Task<List<DocumentCostsListDto>> GetDocumentCostsListAsync(int documentId);
    Task<List<CostTypeComboDto>> GetCostTypesComboAsync();
    Task<List<CostDistributionMethodComboDto>> GetCostDistributionMethodsComboAsync();
    Task<List<CostArticleComboDto>> GetCostArticlesComboAsync(int documentId);
}
