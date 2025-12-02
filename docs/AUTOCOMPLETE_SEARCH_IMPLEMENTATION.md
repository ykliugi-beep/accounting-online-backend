# 🔍 Server-Side Autocomplete Search Implementation Guide

## 📋 Overview

**Problem:** 6000+ Partners and 11000+ Articles causing 30+ second load times and browser timeouts.

**Solution:** Server-side search with autocomplete (minimum 2 characters, max 50 results).

**Technology:** LINQ + EF Core (no Stored Procedures needed).

---

## 📁 Files to Modify/Create

### 1. ✅ `ILookupService.cs` (DONE)

**Path:** `src/ERPAccounting.Application/Services/ILookupService.cs`

**Status:** ✅ Already updated in previous commit

**Changes:**
- Added `SearchPartnersAsync(string searchTerm, int limit = 50)`
- Added `SearchArticlesAsync(string searchTerm, int limit = 50)`

---

### 2. 🔨 `LookupService.cs` (TO DO)

**Path:** `src/ERPAccounting.Application/Services/LookupService.cs`

**Add these two new methods:**

```csharp
using Microsoft.EntityFrameworkCore;
using ERPAccounting.Infrastructure.Data;

public class LookupService : ILookupService
{
    private readonly IStoredProcedureGateway _storedProcedureGateway;
    private readonly AppDbContext _context;  // ← ADD THIS
    private readonly ILogger<LookupService> _logger;  // ← ADD THIS

    public LookupService(
        IStoredProcedureGateway storedProcedureGateway,
        AppDbContext context,  // ← ADD THIS
        ILogger<LookupService> logger)  // ← ADD THIS
    {
        _storedProcedureGateway = storedProcedureGateway;
        _context = context;  // ← ADD THIS
        _logger = logger;  // ← ADD THIS
    }

    // ... existing methods ...

    // ═══════════════════════════════════════════════════════════════
    // NEW METHOD 1: Partner Search
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<PartnerComboDto>> SearchPartnersAsync(
        string searchTerm, 
        int limit = 50)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            _logger.LogDebug("Partner search skipped - search term too short: '{SearchTerm}'", searchTerm);
            return new List<PartnerComboDto>();
        }

        if (limit < 1 || limit > 100)
        {
            limit = 50;  // Default
        }

        var normalizedTerm = searchTerm.Trim().ToLower();

        _logger.LogInformation(
            "Searching partners with term: '{SearchTerm}', limit: {Limit}",
            searchTerm,
            limit);

        try
        {
            // RAW SQL QUERY - direktan pristup tblPartner tabeli
            var sql = @"
                SELECT TOP (@Limit)
                    PartnerID AS IdPartner,
                    Naziv AS NazivPartnera,
                    Mesto,
                    Opis,
                    IdStatus,
                    IdNacinOporezivanjaNabavka,
                    ObracunAkciza,
                    ObracunPorez,
                    IdReferent,
                    Sifra AS SifraPartner
                FROM tblPartner
                WHERE 
                    StatusNabavka = 'Aktivan'
                    AND (
                        LOWER(Sifra) LIKE '%' + @SearchTerm + '%'
                        OR LOWER(Naziv) LIKE '%' + @SearchTerm + '%'
                    )
                ORDER BY Naziv";

            var partners = await _context.Database
                .SqlQueryRaw<PartnerLookupDto>(sql,
                    new Microsoft.Data.SqlClient.SqlParameter("@SearchTerm", normalizedTerm),
                    new Microsoft.Data.SqlClient.SqlParameter("@Limit", limit))
                .ToListAsync();

            _logger.LogInformation(
                "Partner search returned {Count} results for term: '{SearchTerm}'",
                partners.Count,
                searchTerm);

            // Map to PartnerComboDto
            return partners.Select(p => new PartnerComboDto(
                p.IdPartner,
                p.NazivPartnera,
                p.Mesto,
                p.Opis,
                p.IdStatus,
                p.IdNacinOporezivanjaNabavka,
                p.ObracunAkciza,
                p.ObracunPorez,
                p.IdReferent,
                p.SifraPartner
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching partners with term: '{SearchTerm}'", searchTerm);
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // NEW METHOD 2: Article Search
    // ═══════════════════════════════════════════════════════════════
    public async Task<List<ArticleComboDto>> SearchArticlesAsync(
        string searchTerm, 
        int limit = 50)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            _logger.LogDebug("Article search skipped - search term too short: '{SearchTerm}'", searchTerm);
            return new List<ArticleComboDto>();
        }

        if (limit < 1 || limit > 100)
        {
            limit = 50;  // Default
        }

        var normalizedTerm = searchTerm.Trim().ToLower();

        _logger.LogInformation(
            "Searching articles with term: '{SearchTerm}', limit: {Limit}",
            searchTerm,
            limit);

        try
        {
            // RAW SQL QUERY - direktan pristup tblArtikal tabeli
            var sql = @"
                SELECT TOP (@Limit)
                    ArtikalID AS IdArtikal,
                    Sifra AS SifraArtikal,
                    Naziv AS NazivArtikla,
                    JedinicaMere,
                    PoreskaStopaID AS IdPoreskaStopa,
                    ISNULL(ProcenatPoreza, 0) AS ProcenatPoreza,
                    ISNULL(Akciza, 0) AS Akciza,
                    ISNULL(KoeficijentKolicine, 1) AS KoeficijentKolicine,
                    ISNULL(ImaLot, 0) AS ImaLot,
                    ISNULL(OtkupnaCena, 0) AS OtkupnaCena,
                    ISNULL(PoljoprivredniProizvod, 0) AS PoljoprivredniProizvod
                FROM tblArtikal
                WHERE 
                    StatusUlaz = 'Aktivan'
                    AND (
                        LOWER(Sifra) LIKE '%' + @SearchTerm + '%'
                        OR LOWER(Naziv) LIKE '%' + @SearchTerm + '%'
                    )
                ORDER BY Naziv";

            var articles = await _context.Database
                .SqlQueryRaw<ArticleLookupDto>(sql,
                    new Microsoft.Data.SqlClient.SqlParameter("@SearchTerm", normalizedTerm),
                    new Microsoft.Data.SqlClient.SqlParameter("@Limit", limit))
                .ToListAsync();

            _logger.LogInformation(
                "Article search returned {Count} results for term: '{SearchTerm}'",
                articles.Count,
                searchTerm);

            // Map to ArticleComboDto
            return articles.Select(a => new ArticleComboDto(
                a.IdArtikal,
                a.SifraArtikal,
                a.NazivArtikla,
                a.JedinicaMere,
                a.IdPoreskaStopa ?? 0,
                a.ProcenatPoreza,
                a.Akciza,
                a.KoeficijentKolicine,
                a.ImaLot,
                a.OtkupnaCena,
                a.PoljoprivredniProizvod
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching articles with term: '{SearchTerm}'", searchTerm);
            throw;
        }
    }
}
```

---

### 3. 🔨 Create DTO Classes (TO DO)

**Path:** `src/ERPAccounting.Application/DTOs/PartnerLookupDto.cs`

```csharp
namespace ERPAccounting.Application.DTOs;

/// <summary>
/// DTO for Partner search results (maps to tblPartner table).
/// Used internally for SQL query deserialization.
/// </summary>
public class PartnerLookupDto
{
    public int IdPartner { get; set; }
    public string NazivPartnera { get; set; } = string.Empty;
    public string? Mesto { get; set; }
    public string? Opis { get; set; }
    public int? IdStatus { get; set; }
    public int? IdNacinOporezivanjaNabavka { get; set; }
    public bool ObracunAkciza { get; set; }
    public bool ObracunPorez { get; set; }
    public int? IdReferent { get; set; }
    public string? SifraPartner { get; set; }
}
```

**Path:** `src/ERPAccounting.Application/DTOs/ArticleLookupDto.cs`

```csharp
namespace ERPAccounting.Application.DTOs;

/// <summary>
/// DTO for Article search results (maps to tblArtikal table).
/// Used internally for SQL query deserialization.
/// </summary>
public class ArticleLookupDto
{
    public int IdArtikal { get; set; }
    public string SifraArtikal { get; set; } = string.Empty;
    public string NazivArtikla { get; set; } = string.Empty;
    public string? JedinicaMere { get; set; }
    public int? IdPoreskaStopa { get; set; }
    public decimal ProcenatPoreza { get; set; }
    public bool Akciza { get; set; }
    public decimal KoeficijentKolicine { get; set; }
    public bool ImaLot { get; set; }
    public bool OtkupnaCena { get; set; }
    public bool PoljoprivredniProizvod { get; set; }
}
```

---

### 4. 🔨 Update Controller (TO DO)

**Path:** `src/ERPAccounting.API/Controllers/LookupsController.cs`

**Add these two new endpoints:**

```csharp
// ═══════════════════════════════════════════════════════════════
// NEW ENDPOINT 1: Partner Search
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Search partners by code or name (autocomplete).
/// Minimum 2 characters required.
/// </summary>
/// <param name="query">Search term (min 2 characters)</param>
/// <param name="limit">Max results (default: 50, max: 100)</param>
/// <returns>List of partners matching search term</returns>
[HttpGet("partners/search")]
[ProducesResponseType(typeof(List<PartnerComboDto>), 200)]
[ProducesResponseType(400)]
public async Task<ActionResult<List<PartnerComboDto>>> SearchPartners(
    [FromQuery] string query,
    [FromQuery] int limit = 50)
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return BadRequest(new { message = "Query parameter is required" });
    }

    if (query.Length < 2)
    {
        return BadRequest(new { message = "Query must be at least 2 characters" });
    }

    if (limit < 1 || limit > 100)
    {
        return BadRequest(new { message = "Limit must be between 1 and 100" });
    }

    var partners = await _lookupService.SearchPartnersAsync(query, limit);
    return Ok(partners);
}

// ═══════════════════════════════════════════════════════════════
// NEW ENDPOINT 2: Article Search
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Search articles by code or name (autocomplete).
/// Minimum 2 characters required.
/// </summary>
/// <param name="query">Search term (min 2 characters)</param>
/// <param name="limit">Max results (default: 50, max: 100)</param>
/// <returns>List of articles matching search term</returns>
[HttpGet("articles/search")]
[ProducesResponseType(typeof(List<ArticleComboDto>), 200)]
[ProducesResponseType(400)]
public async Task<ActionResult<List<ArticleComboDto>>> SearchArticles(
    [FromQuery] string query,
    [FromQuery] int limit = 50)
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return BadRequest(new { message = "Query parameter is required" });
    }

    if (query.Length < 2)
    {
        return BadRequest(new { message = "Query must be at least 2 characters" });
    }

    if (limit < 1 || limit > 100)
    {
        return BadRequest(new { message = "Limit must be between 1 and 100" });
    }

    var articles = await _lookupService.SearchArticlesAsync(query, limit);
    return Ok(articles);
}

// ═══════════════════════════════════════════════════════════════
// DEPRECATE OLD ENDPOINTS (optional)
// ═══════════════════════════════════════════════════════════════

[HttpGet("partners")]
[Obsolete("Use /partners/search instead for better performance with large datasets")]
public async Task<ActionResult<List<PartnerComboDto>>> GetPartners()
{
    _logger.LogWarning("GET /partners called - consider migrating to /partners/search");
    var partners = await _lookupService.GetPartnerComboAsync();
    return Ok(partners);
}

[HttpGet("articles")]
[Obsolete("Use /articles/search instead for better performance with large datasets")]
public async Task<ActionResult<List<ArticleComboDto>>> GetArticles()
{
    _logger.LogWarning("GET /articles called - consider migrating to /articles/search");
    var articles = await _lookupService.GetArticlesComboAsync();
    return Ok(articles);
}
```

---

## 🧪 Testing

### 1. Start Backend

```bash
cd accounting-online-backend
dotnet run --project src/ERPAccounting.API
```

### 2. Test Endpoints

**Partner Search:**
```bash
curl "http://localhost:5286/api/v1/lookups/partners/search?query=sim&limit=10"
```

**Article Search:**
```bash
curl "http://localhost:5286/api/v1/lookups/articles/search?query=crna&limit=10"
```

**Expected Response:**
```json
[
  {
    "idPartner": 1,
    "nazivPartnera": "Simex DOO",
    "mesto": "Beograd",
    "sifraPartner": "P001"
  }
]
```

### 3. Check Swagger

Отвори: `http://localhost:5286/swagger`

Требао би да видиш нове endpoint-е:
- `GET /api/v1/lookups/partners/search`
- `GET /api/v1/lookups/articles/search`

---

## 📊 Performance Expectations

### Before (Current):
```
GET /api/v1/lookups/partners
→ Returns 6000+ records
→ Response size: 28KB+
→ Response time: 29+ seconds
→ Result: Browser timeout ❌
```

### After (With Search):
```
GET /api/v1/lookups/partners/search?query=sim&limit=50
→ Returns max 50 records
→ Response size: <2KB
→ Response time: <100ms
→ Result: Fast autocomplete ✅
```

---

## ⚠️ Important Notes

1. **SQL Injection Protection:** Користимо `SqlParameter` за parameterized queries
2. **Minimum 2 Characters:** Спречава превише широке претраге
3. **Maximum 100 Results:** Ограничава response size
4. **Case-Insensitive:** `LOWER()` функција за SQL
5. **Wildcard Search:** `LIKE '%term%'` налази подстрингове
6. **Index Recommendation:** Креирај индексе касније за перформансе

---

## 🚀 Next Steps

1. ✅ Backend Implementation (овај PR)
2. 📱 Frontend PR - Autocomplete components
3. 📚 Documentation update

---

**Created:** 2025-12-03  
**Author:** AI Assistant  
**Related Issue:** Dropdown timeout performance
