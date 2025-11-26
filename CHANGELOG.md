# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed - 2025-11-26: Cost DTO API Alignment with Database Schema

#### 🔴 CRITICAL: CreateDocumentCostDto Incorrect Fields

**Problem:**
`CreateDocumentCostDto` i related DTO-ovi nisu bili usaglašeni sa stvarnom strukturom baze podataka i GUI specifikacijom.

**GREŠKA:** DTO je sadržavao polja `AmountNet` i `AmountVat` koja **NE POSTOJE** u tabeli `tblDokumentTroskovi`.

```csharp
// ❌ STARO - NETAČNO
public record CreateDocumentCostDto(
    int PartnerId,
    string DocumentTypeCode,
    decimal AmountNet,       // NE POSTOJI u tblDokumentTroskovi!
    decimal AmountVat,       // NE POSTOJI u tblDokumentTroskovi!
    DateTime DueDate,
    string? Description
);
```

**RAZLOG:** Iznos i PDV se čuvaju u **stavkama troška** (`tblDokumentTroskoviStavka` i `tblDokumentTroskoviStavkaPDV`), **NE u zaglavlju**.

**Database Schema Validation:**
```sql
-- tblDokumentTroskovi (zaglavlje) - NEMA AmountNet/AmountVat!
CREATE TABLE [dbo].[tblDokumentTroskovi](
    [IDDokumentTroskovi] int IDENTITY(1,1),
    [IDDokument] int NOT NULL,
    [IDPartner] int NOT NULL,
    [IDVrstaDokumenta] char(2) NOT NULL,
    [BrojDokumenta] varchar(max) NOT NULL,  -- NEDOSTAJALO u DTO!
    [DatumDPO] datetime NOT NULL,
    [DatumValute] datetime NULL,
    [Opis] varchar(max) NULL,
    [IDStatus] int NOT NULL,                -- NEDOSTAJALO u DTO!
    [IDValuta] int NULL,                    -- NEDOSTAJALO u DTO!
    [Kurs] money NULL,                      -- NEDOSTAJALO u DTO!
    [DokumentTroskoviTimeStamp] timestamp
);

-- tblDokumentTroskoviStavka - OVDE JE IZNOS!
CREATE TABLE [dbo].[tblDokumentTroskoviStavka](
    [IDDokumentTroskoviStavka] int IDENTITY(1,1),
    [IDDokumentTroskovi] int NOT NULL,
    [Iznos] money NOT NULL DEFAULT 0,       -- ✅ OVDE!
    [IDUlazniRacuniIzvedeni] int NOT NULL,
    [IDNacinDeljenjaTroskova] int NOT NULL,
    ...
);

-- tblDokumentTroskoviStavkaPDV - OVDE JE PDV!
CREATE TABLE [dbo].[tblDokumentTroskoviStavkaPDV](
    [IDDokumentTroskoviStavkaPDV] int IDENTITY(1,1),
    [IDDokumentTroskoviStavka] int NOT NULL,
    [IDPoreskaStopa] char(2) NOT NULL,
    [IznosPDV] money NOT NULL DEFAULT 0     -- ✅ OVDE!
);
```

---

**Solution:**

**1. CreateDocumentCostDto - FIXED ✅**
```csharp
public record CreateDocumentCostDto(
    int PartnerId,              // → IDPartner
    string DocumentTypeCode,    // → IDVrstaDokumenta
    string DocumentNumber,      // → BrojDokumenta (DODATO)
    DateTime DueDate,           // → DatumDPO
    DateTime? CurrencyDate,     // → DatumValute (DODATO)
    string? Description,        // → Opis
    int StatusId,               // → IDStatus (DODATO)
    int? CurrencyId,            // → IDValuta (DODATO)
    decimal? ExchangeRate       // → Kurs (DODATO)
);
```
- ❌ Uklonjeno: `AmountNet`, `AmountVat`
- ✅ Dodato: `DocumentNumber`, `CurrencyDate`, `StatusId`, `CurrencyId`, `ExchangeRate`
- ✅ 1:1 mapiranje sa `tblDokumentTroskovi`

**2. UpdateDocumentCostDto - FIXED ✅**
- Identične izmene kao `CreateDocumentCostDto`

**3. DocumentCostDto (Response) - FIXED ✅**
```csharp
public record DocumentCostDto(
    int Id,
    int DocumentId,
    int PartnerId,
    string PartnerName,             // Join (DODATO)
    string DocumentTypeCode,
    string DocumentNumber,          // DODATO
    DateTime DueDate,
    DateTime? CurrencyDate,         // DODATO
    string? Description,
    int StatusId,                   // DODATO
    int? CurrencyId,                // DODATO
    decimal? ExchangeRate,          // DODATO
    decimal TotalAmountNet,         // Calculated: SUM(items.Amount) (DODATO)
    decimal TotalAmountVat,         // Calculated: SUM(items.VatItems.VatAmount) (DODATO)
    List<DocumentCostItemDto> Items, // Child stavke (DODATO)
    string ETag
);
```

**4. CreateDocumentCostItemDto - COMPLETELY REWRITTEN ✅**
```csharp
// ❌ STARO - POTPUNO POGREŠNO
public record CreateDocumentCostItemDto(
    int ArticleId,        // Ne postoji u tblDokumentTroskoviStavka!
    decimal Quantity,
    decimal AmountNet,
    decimal AmountVat,
    int TaxRateId,
    string? Note
);

// ✅ NOVO - TAČNO
public record CreateDocumentCostItemDto(
    int CostTypeId,                // → IDUlazniRacuniIzvedeni (Vrsta troška)
    int DistributionMethodId,      // → IDNacinDeljenjaTroskova (1/2/3)
    decimal Amount,                // → Iznos
    bool ApplyToAllItems,          // → SveStavke
    int StatusId,                  // → IDStatus
    bool CalculateTaxOnCost,       // → ObracunPorezTroskovi
    bool AddVatToCost,             // → DodajPDVNaTroskove
    decimal? CurrencyAmount,       // → IznosValuta
    decimal? CashAmount,           // → Gotovina
    decimal? CardAmount,           // → Kartica
    decimal? WireTransferAmount,   // → Virman
    decimal? Quantity,             // → Kolicina
    List<CostItemVatDto> VatItems  // → tblDokumentTroskoviStavkaPDV
);

public record CostItemVatDto(
    string TaxRateId,    // → IDPoreskaStopa (char(2))
    decimal VatAmount    // → IznosPDV
);
```

**5. DocumentCostItemDto (Response) - COMPLETELY REWRITTEN ✅**
- Dodati svi atributi iz `tblDokumentTroskoviStavka`
- Dodato `VatItems` lista sa PDV stavkama
- Dodato `TotalVat` kao calculated property
- Dodati join nazivi za GUI prikaz

**6. PatchDocumentCostItemDto - FIXED ✅**
- Sva polja opciona (PATCH pattern)
- Odgovara `CreateDocumentCostItemDto` strukturi

---

**API Endpoint Examples:**

```http
# STARO - NETAČNO ❌
POST /api/v1/documents/{documentId}/costs
{
  "partnerId": 123,
  "documentTypeCode": "TR",
  "amountNet": 5000,      // NE POSTOJI!
  "amountVat": 1000,      // NE POSTOJI!
  "dueDate": "2025-11-26",
  "description": "Transport"
}

# NOVO - TAČNO ✅
POST /api/v1/documents/{documentId}/costs
{
  "partnerId": 123,
  "documentTypeCode": "TR",
  "documentNumber": "TR-001/2025",
  "dueDate": "2025-11-26",
  "currencyDate": null,
  "description": "Transport",
  "statusId": 1,
  "currencyId": null,
  "exchangeRate": null
}

POST /api/v1/documents/{documentId}/costs/{costId}/items
{
  "costTypeId": 5,              // Transport
  "distributionMethodId": 2,    // Po vrednosti
  "amount": 6000,
  "applyToAllItems": true,
  "statusId": 1,
  "calculateTaxOnCost": true,
  "addVatToCost": false,
  "vatItems": [
    { "taxRateId": "01", "vatAmount": 1200 }
  ]
}
```

---

**GUI Alignment:**

Nove DTO strukture sada **potpuno odgovaraju** ERP-SPECIFIKACIJA.docx:

**TAB ZAVISNI TROSKOVI:**
```
GORNJI GRID (tblDokumentTroskovi):
  ✅ ANALITIKA (Partner)
  ✅ VRSTA DOKUMENTA
  ✅ BROJ DOKUMENTA
  ✅ DATUM DPO
  ✅ DATUM VALUTE
  ✅ OPIS
  ✅ Ukupan iznos (calculated)

DONJI GRID (tblDokumentTroskoviStavka):
  ✅ VRSTA TROSKA (CostTypeId)
  ✅ NACIN DELJENJA (DistributionMethodId)
  ✅ IZNOS (Amount)
  ✅ PDV GRID (VatItems)
     ✅ PORESKA STOPA
     ✅ IZNOS PDV
```

---

**Impact:**
- ⚠️ **Breaking Changes:** YES
- ⚠️ **Frontend update required:** YES
- ⚠️ **Service layer update required:** YES
- ✅ **Database changes:** NONE (schema ostaje ista)

**Files Changed:**
1. `src/ERPAccounting.Application/DTOs/Costs/CreateDocumentCostDto.cs`
2. `src/ERPAccounting.Application/DTOs/Costs/UpdateDocumentCostDto.cs`
3. `src/ERPAccounting.Application/DTOs/Costs/DocumentCostDto.cs`
4. `src/ERPAccounting.Application/DTOs/Costs/CreateDocumentCostItemDto.cs`
5. `src/ERPAccounting.Application/DTOs/Costs/DocumentCostItemDto.cs`
6. `src/ERPAccounting.Application/DTOs/Costs/PatchDocumentCostItemDto.cs`

**References:**
- Database Schema: `docs/database-structure/tblDocuments.txt`
- GUI Specification: `ERP-SPECIFIKACIJA.docx`
- Stored Procedures: `docs/database-structure/spDocuments.txt`

---

### Fixed - 2025-11-24

#### 🔴 CRITICAL: Remove IsDeleted and Audit Fields (PR #1)

**Problem:**
- `Invalid column name 'IsDeleted'` SQL exception
- `Invalid column name 'Napomena'` in DocumentCostLineItem
- Query filters attempting to access non-existent database columns
- BaseEntity audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) marked as [NotMapped] but still causing issues

**Root Cause:**
- Entities inherited from `BaseEntity` which had audit tracking fields
- `ISoftDeletable` interface added `IsDeleted` property to entities
- Global query filter in `AppDbContext` tried to filter by `IsDeleted` column
- Database schema does NOT contain these columns - they were never migrated

**Solution:**
1. **Removed `BaseEntity.cs`** - All audit fields deleted
2. **Removed `ISoftDeletable.cs` interface** - Soft delete will be tracked via Audit tables only
3. **Updated all entities:**
   - `Document.cs` - removed `: BaseEntity, ISoftDeletable`, removed `IsDeleted` property
   - `DocumentLineItem.cs` - removed `: BaseEntity, ISoftDeletable`, removed `IsDeleted` property
   - `DocumentCost.cs` - removed `: BaseEntity`, removed audit fields
   - `DocumentCostLineItem.cs` - removed `: BaseEntity`, removed `Napomena` property (does not exist in tblDokumentTroskoviStavka)
   - `DocumentAdvanceVAT.cs` - removed `: BaseEntity`
   - `DependentCostLineItem.cs` - removed `: BaseEntity`
   - `DocumentCostVAT.cs` - removed `: BaseEntity`

4. **Updated `AppDbContext.cs`:**
   - Removed global query filter loop for `ISoftDeletable`
   - Removed `.Property<bool>("IsDeleted")` configuration
   - Removed `.HasQueryFilter()` calls for soft delete

5. **Updated all Repositories:**
   - Removed `.Where(x => !x.IsDeleted)` clauses
   - Removed `.Where(x => x.IsDeleted == false)` clauses

**Impact:**
- ✅ All Swagger endpoints now work without SQL exceptions
- ✅ Entity models map 1:1 to existing database tables
- ✅ No migrations needed - database unchanged
- ✅ Audit trail still works via dedicated `tblAPIAuditLog` and `tblAPIAuditLogEntityChanges` tables

**Migration Path:**
- **Soft Delete:** Tracked via `ApiAuditLogEntityChange` with `ChangeType = 'DELETE'`
- **Audit Fields:** Tracked via `ApiAuditLog.Username`, `ApiAuditLog.Timestamp`
- **Entity State:** Use EF Core `EntityState.Deleted` for soft delete logic in services

**Files Changed:**
```
DELETED:
  src/ERPAccounting.Domain/Entities/BaseEntity.cs
  src/ERPAccounting.Domain/Interfaces/ISoftDeletable.cs

MODIFIED:
  src/ERPAccounting.Domain/Entities/Document.cs
  src/ERPAccounting.Domain/Entities/DocumentLineItem.cs
  src/ERPAccounting.Domain/Entities/DocumentCost.cs
  src/ERPAccounting.Domain/Entities/DocumentCostLineItem.cs
  src/ERPAccounting.Domain/Entities/DocumentAdvanceVAT.cs
  src/ERPAccounting.Domain/Entities/DependentCostLineItem.cs
  src/ERPAccounting.Domain/Entities/DocumentCostVAT.cs
  src/ERPAccounting.Infrastructure/Data/AppDbContext.cs
  src/ERPAccounting.Infrastructure/Repositories/DocumentRepository.cs
  src/ERPAccounting.Infrastructure/Repositories/DocumentLineItemRepository.cs
  src/ERPAccounting.Infrastructure/Repositories/DocumentCostRepository.cs
  src/ERPAccounting.Infrastructure/Repositories/DocumentCostLineItemRepository.cs
```

**Testing:**
- [x] Swagger GET /api/v1/documents - 200 OK
- [x] Swagger GET /api/v1/documents/{id} - 200 OK
- [x] Swagger GET /api/v1/documents/{id}/items - 200 OK
- [x] Swagger GET /api/v1/documents/{id}/costs - 200 OK
- [x] No SQL exceptions in logs
- [x] ETag still works via RowVersion (DokumentTimeStamp, StavkaDokumentaTimeStamp)

**Breaking Changes:**
- ⚠️ Any code that directly accessed `IsDeleted` property must be refactored
- ⚠️ Any code that accessed `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` must use Audit tables

**Database Changes:**
- ✅ **NONE** - This is purely code refactoring to match existing schema

---

## [0.1.0] - 2025-11-20

### Added
- Initial project setup
- Clean Architecture structure
- Entity Framework Core 8.0 configuration
- Basic entity models
- API Controllers scaffolding
- Swagger/OpenAPI documentation

---

## Template for Future Changes

```markdown
## [Version] - YYYY-MM-DD

### Added
- New features

### Changed
- Changes in existing functionality

### Deprecated
- Soon-to-be removed features

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Security improvements
```