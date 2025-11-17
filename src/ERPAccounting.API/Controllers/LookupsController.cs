using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERPAccounting.Application.Services;
using ERPAccounting.Application.DTOs;

namespace ERPAccounting.API.Controllers
{
    /// <summary>
    /// Lookup Controller - Svi combo endpointi za popunjavanje dropdowns
    /// Koristi 11 Stored Procedures iz baze
    /// </summary>
    [ApiController]
    [Route("api/v1/lookups")]
    [Authorize]
    public class LookupsController : ControllerBase
    {
        private readonly IStoredProcedureService _spService;
        private readonly ILogger<LookupsController> _logger;

        public LookupsController(
            IStoredProcedureService spService,
            ILogger<LookupsController> logger)
        {
            _spService = spService;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════
        // SP 1: spPartnerComboStatusNabavka
        /// <summary>
        /// GET /api/v1/lookups/partners
        /// Vraća sve partnere za nabavke
        /// </summary>
        [HttpGet("partners")]
        [ProducesResponseType(typeof(List<PartnerComboDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PartnerComboDto>>> GetPartners()
        {
            try
            {
                var result = await _spService.GetPartnerComboAsync();
                _logger.LogInformation("Partners loaded: {Count}", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading partners");
                return StatusCode(500, new { message = "Greška pri učitavanju partnera" });
            }
        }

        // ══════════════════════════════════════════════════
        // SP 2: spOrganizacionaJedinicaCombo
        /// <summary>
        /// GET /api/v1/lookups/organizational-units?docTypeId=UR
        /// Vraća organizacione jedinice za određenu vrstu dokumenta
        /// </summary>
        [HttpGet("organizational-units")]
        [ProducesResponseType(typeof(List<OrgUnitComboDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<OrgUnitComboDto>>> GetOrgUnits([FromQuery] string? docTypeId = null)
        {
            try
            {
                docTypeId = docTypeId ?? "UR"; // Default
                var result = await _spService.GetOrgUnitsComboAsync(docTypeId);
                _logger.LogInformation("Organizational units loaded for {DocType}: {Count}", docTypeId, result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading organizational units");
                return StatusCode(500, new { message = "Greška pri učitavanju org. jedinica" });
            }
        }

        // ══════════════════════════════════════════════════
        // SP 3: spNacinOporezivanjaComboNabavka
        /// <summary>
        /// GET /api/v1/lookups/taxation-methods
        /// Vraća sve načine oporezivanja
        /// </summary>
        [HttpGet("taxation-methods")]
        [ProducesResponseType(typeof(List<TaxationMethodComboDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TaxationMethodComboDto>>> GetTaxationMethods()
        {
            try
            {
                var result = await _spService.GetTaxationMethodsComboAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading taxation methods");
                return StatusCode(500, new { message = "Greška pri učitavanju načina oporezivanja" });
            }
        }

        // ══════════════════════════════════════════════════
        // SP 4: spReferentCombo
        /// <summary>
        /// GET /api/v1/lookups/referents
        /// Vraća sve referente (zaposlene)
        /// </summary>
        [HttpGet("referents")]
        [ProducesResponseType(typeof(List<ReferentComboDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ReferentComboDto>>> GetReferents()
        {
            try
            {
                var result = await _spService.GetReferentsComboAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading referents");
                return StatusCode(500, new { message = "Greška pri učitavanju referenata" });
            }
        }

        // ══════════════════════════════════════════════════
        // SP 5: spDokumentNDCombo
        /// <summary>
        /// GET /api/v1/lookups/documents-nd
        /// Vraća sve ND dokumente kao referentne
        /// </summary>
        [HttpGet("documents-nd")]
        [ProducesResponseType(typeof(List<DocumentNDComboDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DocumentNDComboDto>>> GetDocumentsND()
        {
            try
            {
                var result = await _spService.GetDocumentNDComboAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ND documents");
                return StatusCode(500, new { message = "Greška pri učitavanju ND dokumenata" });
            }
        }

        // ══════════════════════════════════════════════════
        // SP 6: spPoreskaStopaCombo
        /// <summary>
        /// GET /api/v1/lookups/tax-rates
        /// Vraća sve poreske stope
        /// </summary>
        [HttpGet("tax-rates")]
        [ProducesResponseType(typeof(List<TaxRateComboDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<TaxRateComboDto>>> GetTaxRates()
        {
            try
            {
                var result = await _spService.GetTaxRatesComboAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tax rates");
                return StatusCode(500, new { message = "Greška pri učitavanju poreskih stopa" });
            }
        }

        // ══════════════════════════════════════════════════
        // SP 7: spArtikalComboUlaz
        /// <summary>
        /// GET /api/v1/lookups/articles
        /// Vraća sve artikle za nabavke
        /// </summary>
        [HttpGet("articles")]
        [ProducesResponseType(typeof(List<ArticleComboDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ArticleComboDto>>> GetArticles()
        {
            try
            {
                var result = await _spService.GetArticlesComboAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading articles");
                return StatusCode(500, new { message = "Greška pri učitavanju artikala" });
            }
        }

        // ══════════════════════════════════════════════════
        // SP 8: spDokumentTroskoviLista
        /// <summary>
        /// GET /api/v1/lookups/document-costs/{documentId}
        /// Vraća sve troškove za određeni dokument
        /// </summary>
        [HttpGet("document-costs/{documentId:int}")]
        [ProducesResponseType(typeof(List<DocumentCostsListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DocumentCostsListDto>>> GetDocumentCosts(int documentId)
        {
            try
            {
                var result = await _spService.GetDocumentCostsListAsync(documentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading document costs for {DocumentId}", documentId);
                return StatusCode(500, new { message = "Greška pri učitavanju troškova" });
            }
        }

        // ══════════════════════════════════════════════════
        // SP 9: spUlazniRacuniIzvedeniTroskoviCombo
        /// <summary>
        /// GET /api/v1/lookups/cost-types
        /// Vraća sve vrste troškova
        /// </summary>
        [HttpGet("cost-types")]
        [ProducesResponseType(typeof(List<CostTypeComboDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CostTypeComboDto>>> GetCostTypes()
        {
            try
            {
                var result = await _spService.GetCostTypesComboAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cost types");
                return StatusCode(500, new { message = "Greška pri učitavanju vrsta troškova" });
            }
        }

        // ══════════════════════════════════════════════════
        // SP 10: spNacinDeljenjaTroskovaCombo
        /// <summary>
        /// GET /api/v1/lookups/cost-distribution-methods
        /// Vraća sve načine raspoređivanja troškova (1, 2, 3)
        /// </summary>
        [HttpGet("cost-distribution-methods")]
        [ProducesResponseType(typeof(List<CostDistributionMethodComboDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CostDistributionMethodComboDto>>> GetCostDistributionMethods()
        {
            try
            {
                var result = await _spService.GetCostDistributionMethodsComboAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cost distribution methods");
                return StatusCode(500, new { message = "Greška pri učitavanju načina raspoređivanja" });
            }
        }

        // ══════════════════════════════════════════════════
        // SP 11: spDokumentTroskoviArtikliCOMBO
        /// <summary>
        /// GET /api/v1/lookups/cost-articles/{documentId}
        /// Vraća sve artikle iz stavki dokumenta za raspoređivanje troškova
        /// </summary>
        [HttpGet("cost-articles/{documentId:int}")]
        [ProducesResponseType(typeof(List<CostArticleComboDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CostArticleComboDto>>> GetCostArticles(int documentId)
        {
            try
            {
                var result = await _spService.GetCostArticlesComboAsync(documentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cost articles for {DocumentId}", documentId);
                return StatusCode(500, new { message = "Greška pri učitavanju artikala za troškove" });
            }
        }
    }
}
