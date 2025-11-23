# 👨‍💻 BACKEND AGENT.md v3.1 - FINALNI MASTER - SVE IZ v2 + v3 KOMPLET

**Verzija:** 3.1 - FINALNA - SVE OBUHVAĆENO  
**Kreirano:** 16.11.2025  
**Projekat:** ERP Accounting Backend - Excel-like Unos  
**Stack:** .NET 8.0 LTS + ASP.NET Core + Entity Framework Core + SQL Server

---

## 📊 MAPIRANJE BAZE → C# TIPOVI (TAČNO IZ BAZE!)

```
SQL Type          →  C# Type        →  EF Konfiguracija
====================================================
int               →  int            →  .HasColumnType("int")
char(2)           →  string?        →  .HasMaxLength(2)
varchar(n)        →  string?        →  .HasMaxLength(n)
varchar(max)      →  string?        →  .HasColumnType("varchar(max)")
datetime          →  DateTime?      →  .HasColumnType("datetime")
money             →  decimal        →  .HasColumnType("money").HasPrecision(19,4)
float             →  double         →  .HasColumnType("float")
bit               →  bool           →  .HasColumnType("bit")
smallint          →  short/bool     →  .HasColumnType("smallint")
IDENTITY(1,1)     →  [DatabaseGenerated] → .ValueGeneratedOnAdd()
timestamp         →  byte[]         →  [Timestamp] / .IsRowVersion()
UNIQUE            →  .HasIndex()    →  .IsUnique()
FK CASCADE        →  .OnDelete(DeleteBehavior.Cascade)
CHECK <> 0        →  FluentValidation → .GreaterThan(0)
COMPUTED          →  [NotMapped]    →  public decimal Computed => ...
```

---

## 🏛️ CLEAN ARCHITECTURE - 4 SLOJA (OBAVEZNO!)

```
ERPAccounting.API                    → Controllers, Middleware, Program.cs
├── Controllers/
│   ├── DocumentsController.cs
│   ├── DocumentItemsController.cs   ← KRITIČNO: ETag
│   ├── DocumentCostsController.cs   ← KRITIČNO: ETag
│   └── LookupsController.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   ├── TenantResolutionMiddleware.cs
│   └── RequestLoggingMiddleware.cs
└── appsettings.json

ERPAccounting.Application            → Services, DTOs, Validators
├── Services/
│   ├── IDocumentService.cs + DocumentService.cs
│   ├── IDocumentItemService.cs + DocumentItemService.cs ← KONKURENTNOST!
│   ├── IDocumentCostService.cs + DocumentCostService.cs
│   ├── ILookupService.cs + LookupService.cs
│   ├── ICostDistributionService.cs + CostDistributionService.cs
│   └── IStoredProcedureService.cs + StoredProcedureService.cs
├── DTOs/
│   ├── DocumentDtos.cs
│   ├── DocumentItemDtos.cs (CREATE/PATCH/RESPONSE)
│   ├── DocumentCostDtos.cs (CREATE/PATCH/RESPONSE)
│   ├── ComboDtos.cs (svi 11 SP-a)
│   ├── PaginationDto.cs
│   └── ApiResponseDtos.cs
├── Validators/
│   ├── CreateDocumentValidator.cs
│   ├── CreateLineItemValidator.cs
│   ├── PatchLineItemValidator.cs    ← KONKURENTNOST!
│   ├── PatchCostValidator.cs        ← KONKURENTNOST!
│   └── CreateCostValidator.cs
├── Mapping/
│   └── MappingProfile.cs (AutoMapper)
└── Extensions/
    └── ServiceCollectionExtensions.cs

ERPAccounting.Domain                 → Entities sa RowVersion
├── Entities/
│   ├── Document.cs (86 svojstava)
│   ├── DocumentLineItem.cs (65 svojstava) ← RowVersion!
│   ├── DocumentCost.cs
│   ├── DocumentCostLineItem.cs (14 svojstava) ← RowVersion!
│   ├── Partner.cs (37 svojstava)
│   ├── OrganizationalUnit.cs (26 svojstava)
│   ├── Article.cs
│   ├── TaxRate.cs
│   └── ... (ostale reference)
├── ValueObjects/
│   ├── Money.cs
│   ├── DocumentStatus.cs
│   └── TaxInfo.cs
├── Enums/
│   ├── DocumentType.cs
│   ├── OperationType.cs
│   └── CostDistributionMethod.cs (1, 2, 3)
└── Interfaces/
    ├── IAggregateRoot.cs
    ├── IEntity.cs
    └── IAuditableEntity.cs

ERPAccounting.Infrastructure         → Repositories, DbContext, SP
├── Data/
│   ├── AppDbContext.cs (sa svim OnModelCreating)
│   ├── Migrations/
│   │   └── 20251116_InitialCreate.cs
│   └── ModelConfiguration/
│       ├── DocumentConfiguration.cs
│       ├── LineItemConfiguration.cs (RowVersion!)
│       ├── CostConfiguration.cs (RowVersion!)
│       └── ... (ostale)
├── Repositories/
│   ├── IRepository.cs + Repository.cs (Generic)
│   ├── IDocumentRepository.cs + DocumentRepository.cs
│   ├── ILineItemRepository.cs + LineItemRepository.cs
│   ├── ICostRepository.cs + CostRepository.cs
│   └── IUnitOfWork.cs + UnitOfWork.cs
├── Services/
│   ├── StoredProcedureService.cs (sve 11 SP-a)
│   └── CacheService.cs
└── Extensions/
    └── InfrastructureServiceCollectionExtensions.cs

ERPAccounting.Common                 → Exceptions, Constants
├── Exceptions/
│   ├── DomainException.cs
│   ├── NotFoundException.cs
│   ├── ConflictException.cs (409 - OBAVEZNO!)
│   └── ValidationException.cs
├── Constants/
│   ├── ApiRoutes.cs
│   ├── ErrorMessages.cs
│   └── CacheKeys.cs
└── Models/
    ├── ProblemDetailsDto.cs
    └── ConflictDetailsDto.cs (za 409)
```

---

## 📋 ENTITETI - SVA SVOJSTVA IZ BAZE (tačno mapovano)

### DocumentLineItem - 65 svojstava

```csharp
public class DocumentLineItem
{
    // PK i FK - OBAVEZNO int (iz baze!)
    public int Id { get; set; }  // IDStavkaDokumenta (IDENTITY)
    public int DocumentId { get; set; }  // IDDokument (FK, CASCADE)
    public int ArticleId { get; set; }  // IDArtikal (OBAVEZNO)
    public int? OrganizationalUnitId { get; set; }  // IDOrganizacionaJedinica
    
    // KOLIČINE I CENE - money tipovi
    public decimal Quantity { get; set; }  // Kolicina (money, CHECK <> 0)
    public decimal InvoicePrice { get; set; }  // FakturnaCena (money)
    public decimal PurchasePrice { get; set; }  // NabavnaCena (money)
    public decimal WarehousePrice { get; set; }  // MagacinskaCena (money)
    public decimal DocumentDiscount { get; set; }  // RabatDokument
    public decimal ActiveMatterPercent { get; set; }  // ProcenatAktivneMaterije
    public decimal Volume { get; set; }  // Zapremina
    public decimal Excise { get; set; }  // Akciza (po JM)
    public decimal QuantityCoefficient { get; set; }  // KoeficijentKolicine (def 1)
    public decimal DiscountAmount { get; set; }  // Rabat (iznos)
    public decimal MarginAmount { get; set; }  // Marza (%)
    public decimal MarginValue { get; set; }  // IznosMarze
    
    // PDV I AKCIZA
    public decimal TaxPercent { get; set; }  // ProcenatPoreza (%)
    public decimal TaxPercentMP { get; set; }  // ProcenatPorezaMP
    public decimal TaxAmount { get; set; }  // IznosPDV
    public decimal TaxAmountWithExcise { get; set; }  // IznosPDVsaAkcizom
    public decimal ExciseAmount { get; set; }  // IznosAkciza
    public string? TaxRateId { get; set; }  // IDPoreskaStopa (char(2), FK)
    
    // ZAVISNI TROŠKOVI
    public decimal DependentCostsWithTax { get; set; }  // ZavisniTroskovi (sa PDV)
    public decimal DependentCostsWithoutTax { get; set; }  // ZavisniTroskoviBezPoreza
    
    // UKUPNI IZNOSI
    public decimal Total { get; set; }  // Iznos (COMPUTED)
    public decimal CurrencyPrice { get; set; }  // ValutaCena
    public decimal CurrencyTotal { get; set; }  // ValutaIznos
    
    // JM I PAKOVANJE
    public string UnitOfMeasureId { get; set; }  // IDJedinicaMere (FK, OBAVEZNO)
    public int Packaging { get; set; }  // Pakovanje
    
    // OBRAČUNI
    public bool CalculateExcise { get; set; }  // ObracunAkciza (smallint → bool)
    public bool CalculateTax { get; set; }  // ObracunPorez (smallint → bool)
    public bool CalculateAuxiliaryTax { get; set; }  // ObracunPorezPomocni
    public int? TaxationMethodId { get; set; }  // IDNacinOporezivanja (FK)
    public int? StatusId { get; set; }  // IDStatus (FK)
    
    // MASA I OPIS
    public decimal Weight { get; set; }  // Masa
    public string? Description { get; set; }  // Opis
    
    // PROIZVODNJA
    public double ProductionQuantity { get; set; }  // ProizvodnjaKolicina (float)
    public string? ProductionUnitOfMeasureId { get; set; }  // ProizvodnjaIDJedinicaMere
    public double ProductionQuantityCoefficient { get; set; }  // ProizvodnjaKoef (float)
    public int? MealOrderLineId { get; set; }  // IDObrociNarudzbinaStavka
    public int? MealTypeId { get; set; }  // IDVrstaObroka
    
    // DNEVNA STANJA
    public int DailyInventoryChangeM1 { get; set; }  // IDDnevnaStanjaMagacinskoPromeneM1
    public int DailyInventoryChangeM2 { get; set; }  // IDDnevnaStanjaMagacinskoPromeneM2
    public int DailyGoodsChangeM1 { get; set; }  // IDDnevnaStanjaRobnoPromeneM1
    public int DailyGoodsChangeM2 { get; set; }  // IDDnevnaStanjaRobnoPromeneM2
    public int DailyVPChangeM1 { get; set; }  // IDDnevnaStanjaVPPromeneM1
    public int DailyVPChangeM2 { get; set; }  // IDDnevnaStanjaVPPromeneM2
    
    // DODATNI RABATI I CENE
    public int? BaseAccountId { get; set; }  // IDUlazniRacuniOsnovni
    public decimal ActionDiscount { get; set; }  // RabatAkcija
    public bool? DeliveryOfGoods { get; set; }  // IsporukaRobe
    public decimal Discount2 { get; set; }  // Rabat2
    public decimal? LastPurchasePrice { get; set; }  // ZadnjaNabavnaCena
    public decimal? AveragePrice { get; set; }  // ProsecnaCena
    
    // VALUTA
    public int? CurrencyDays { get; set; }  // ValutaBrojDana
    public DateTime? CurrencyDate { get; set; }  // ValutaDatum
    public decimal? PriceWithoutTax { get; set; }  // VrednostBezPDV
    
    // OPREMA
    public string? MandatoryEquipment { get; set; }  // ObaveznaOprema
    public string? SupplementaryEquipment { get; set; }  // DopunskaOprema
    public decimal? AveragePriceUJ { get; set; }  // ProsecnaCenaOJ
    public decimal? ReturnAmount { get; set; }  // PovratnaNaknada
    public decimal? OldPrice { get; set; }  // StaraCena
    public int? ColorId { get; set; }  // IDBoja
    
    // ═══════════════════════════════════════════════════════════════
    // KONKURENTNOST - OBAVEZNO! (tblStavkaDokumenta.StavkaDokumentaTimeStamp)
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    // ═══════════════════════════════════════════════════════════════
    
    // AUDIT
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    
    // SOFT DELETE
    public bool IsDeleted { get; set; }
    
    // NAVIGATION
    public virtual Document Document { get; set; } = null!;
    public virtual Article Article { get; set; } = null!;
    public virtual UnitOfMeasure? UnitOfMeasure { get; set; }
    public virtual TaxRate? TaxRate { get; set; }
    public virtual TaxationMethod? TaxationMethod { get; set; }
    public virtual Status? Status { get; set; }
}
```

### Document - 86 svojstava (SAMO KLJUČNA - ostalo kao FK reference)

```csharp
public class Document
{
    // IDENTITET
    public int Id { get; set; }  // IDDokument (IDENTITY)
    public string DocumentTypeId { get; set; }  // IDVrstaDokumenta (char(2), FK)
    public string DocumentNumber { get; set; }  // BrojDokumenta (OBAVEZNO)
    public int DocumentNumberInt { get; set; }  // BrojDokumentaINT
    
    // DATUMI
    public int? Year { get; set; }  // Godina
    public DateTime DocumentDate { get; set; }  // Datum (OBAVEZNO)
    public DateTime? ValueDate { get; set; }  // DatumValute
    public DateTime? ReceiptDate { get; set; }  // DatumDPO
    
    // PARTNERI
    public int? PartnerId { get; set; }  // IDPartner (FK)
    public int OrganizationalUnitId { get; set; }  // IDOrganizacionaJedinica (FK, OBAVEZNO)
    public int? InternalPartnerId { get; set; }  // IDInterniPartner (FK)
    public string? PartnerDocumentNumber { get; set; }  // PartnerBrojDokumenta
    public DateTime? PartnerDocumentDate { get; set; }  // PartnerDatumDokumenta
    
    // REFERENT
    public int? EmployeeId { get; set; }  // IDRadnik (FK)
    public int? ReferenceDocumentId { get; set; }  // IDReferentniDokument (FK, self)
    
    // BELEŠKE
    public string? Notes { get; set; }  // Napomena
    public string? SystemNotes { get; set; }  // NapomenaSystem
    
    // STATUS
    public bool IsProcessed { get; set; }  // ObradjenDokument (bit)
    public bool IsPosted { get; set; }  // ProknjizenDokument (bit)
    public bool IsLocked { get; set; }  // ZakljucanDokument (bit)
    
    // AUDIT
    public string? CreatedBy { get; set; }  // UserName
    public string? CreatedLocation { get; set; }  // UserLokacija
    public DateTime? CreatedAt { get; set; }  // UserDatum
    public string? ConfirmedBy { get; set; }  // UserNameK
    public string? ConfirmedLocation { get; set; }  // UserLokacijaK
    public DateTime? ConfirmedAt { get; set; }  // UserDatumK
    
    // NAČIN I OBRAČUNI
    public int? PaymentMethodId { get; set; }  // IDNacinPlacanja (FK)
    public int? TaxationMethodId { get; set; }  // IDNacinOporezivanja (FK)
    public int? StatusId { get; set; }  // IDStatus (FK)
    public bool CalculateExcise { get; set; }  // ObracunAkciza (smallint → bool)
    public bool CalculateTax { get; set; }  // ObracunPorez (smallint → bool)
    public bool CalculateAuxiliaryTax { get; set; }  // ObracunPorezPomocni
    
    // VALUTA
    public int? CurrencyId { get; set; }  // IDValuta (FK)
    public decimal ExchangeRate { get; set; }  // KursValute (def 0)
    public decimal AdvanceAmount { get; set; }  // AvansIznos (def 0)
    
    // KONTIRANJE
    public int? AccountingModelId { get; set; }  // IDModelKontiranja
    public int? DeliveryLocationId { get; set; }  // IDMestoIsporuke
    public int? RequiredArticleId { get; set; }  // TrebovanjeIDArtikal
    public decimal RequiredQuantity { get; set; }  // TrebovanjeKolicina
    
    // TROŠKOVI
    public decimal PreferingAmount { get; set; }  // IznosPrevaranti
    public decimal DependentCostsWithoutTax { get; set; }  // ZavisniTroskoviBezPDVa
    public decimal DependentCostsWithTax { get; set; }  // ZavisniTroskoviPDV
    public int? CostPlaceId { get; set; }  // IDTroskovnoMesto
    
    // TRANSPORT
    public int? DriverId { get; set; }  // IDVozac (FK)
    public int? VehicleId { get; set; }  // IDVozilo (FK)
    public decimal? Mileage { get; set; }  // Kilometraza
    public string? Registration { get; set; }  // Registracija
    public int? TrailerId { get; set; }  // IDPrikolica
    
    // PROIZVODNJA
    public int? ProductionLineId { get; set; }  // IDLinijaProizvodnje
    public int? InternalAccountPurposeId { get; set; }  // IDSvrhaInternihRacuna
    
    // IZNOSI
    public decimal? GrossAmount { get; set; }  // Bruto
    public decimal? NetAmount { get; set; }  // Neto
    public string? BorderCrossing { get; set; }  // GranicniPrelaz
    
    // VRAĆANJA
    public int? ReturnedDocumentId { get; set; }  // IDStorniranogDokumenta
    public int? BaseAccountId { get; set; }  // IDUlazniRacuniOsnovni
    
    // PLAĆANJA
    public decimal CheckAmount { get; set; }  // IznosCek (def 0)
    public decimal CardAmount { get; set; }  // IznosKartica (def 0)
    public decimal CashAmount { get; set; }  // IznosGotovina (def 0)
    
    // OTPREMA
    public string? TravelOrderNumber { get; set; }  // BrojPutnogNaloga
    public bool? IsDispatched { get; set; }  // Otpremljeno
    public string? DeliveryTime { get; set; }  // VremeRazvoza
    public string? AlternativeDocumentNumber { get; set; }  // BrojDokAlt
    public string? AdditionalNotes2 { get; set; }  // Napomena2
    public string? AdditionalNotes3 { get; set; }  // Napomena3
    
    // SINHRONIZACIJA
    public bool IsSyncedWithAccess { get; set; }  // SinhronizovanAccess
    public bool HasError { get; set; }  // Feler
    public string? AdditionalApprovalIndicator { get; set; }  // IndikatorNaknadnogOdobrenja
    public string? ApprovedAdditionalDelivery { get; set; }  // OdobrioNaknadnuIsporuku
    public string? MetroName { get; set; }  // ImePrezimeMetro
    
    // NARUDŽBENI
    public string? OrderNumber { get; set; }  // BrojNarudzbenice
    public string? StoreNumber { get; set; }  // BrojProdavnice
    public DateTime? OrderDate { get; set; }  // DatumNarudzbenice
    
    // BANKE
    public int? CurrentAccountId { get; set; }  // IDTekuciRacun
    public string? ReferenceNumber { get; set; }  // PozivNaBroj
    public decimal? InvoiceValue { get; set; }  // VrednostSaRacuna
    public string? ReferenceNumber2 { get; set; }  // PozivNaBroj1
    public DateTime? PaymentDueDate { get; set; }  // Rok
    
    // KONTAKT
    public string? ContactPerson { get; set; }  // Kontakt
    
    // DODATNI
    public int? AdditionalEmployeeId { get; set; }  // IDRadnik2 (FK)
    public decimal? AdditionalWorkAmount { get; set; }  // DodatniRadoviIznos
    public int? AdditionalPartnerId { get; set; }  // IDPartner2 (FK)
    public int? CostTypeId { get; set; }  // IDVrstaTroska
    public int? Location1Id { get; set; }  // IDMesto1
    public int? Location2Id { get; set; }  // IDMesto2
    public int? MeasurementId { get; set; }  // IDMerenje
    
    // ═══════════════════════════════════════════════════════════════
    // KONKURENTNOST - OBAVEZNO! (tblDokument.DokumentTimeStamp)
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    // ═══════════════════════════════════════════════════════════════
    
    // NAVIGATION
    public virtual ICollection<DocumentLineItem> Items { get; } = new List<DocumentLineItem>();
    public virtual Partner? Partner { get; set; }
    public virtual OrganizationalUnit OrganizationalUnit { get; set; } = null!;
}
```

---

## 📋 DTOs - TRI FORME (Create/Patch/Response) - SAMO OBAVEZNA POLJA

### DocumentLineItem DTOs

```csharp
// CREATE - Za POST /documents/{id}/items
public record CreateLineItemDto(
    int ArticleId,              // Obavezno
    decimal Quantity,           // Obavezno (CHECK <> 0)
    decimal InvoicePrice,       // Obavezno
    decimal DiscountAmount,     // 0 ako nije
    decimal MarginAmount,       // 0 ako nije
    string? TaxRateId,         // char(2), FK
    bool CalculateExcise,      // 0/1
    bool CalculateTax          // 0/1
);

// PATCH - Za PATCH /documents/{id}/items/{itemId} (sa If-Match header!)
public record PatchLineItemDto(
    decimal? Quantity,
    decimal? InvoicePrice,
    decimal? DiscountAmount,
    decimal? MarginAmount,
    string? TaxRateId,
    bool? CalculateExcise,
    bool? CalculateTax
);

// RESPONSE - Za sve GET operacije (sa svim poljima)
public record DocumentLineItemDto(
    int Id,
    int DocumentId,
    int ArticleId,
    decimal Quantity,
    decimal InvoicePrice,
    decimal PurchasePrice,
    decimal WarehousePrice,
    decimal DocumentDiscount,
    decimal ActiveMatterPercent,
    decimal Volume,
    decimal Excise,
    decimal QuantityCoefficient,
    decimal DiscountAmount,
    decimal MarginAmount,
    decimal MarginValue,
    decimal TaxPercent,
    decimal TaxPercentMP,
    decimal TaxAmount,
    decimal TaxAmountWithExcise,
    decimal ExciseAmount,
    string? TaxRateId,
    decimal DependentCostsWithTax,
    decimal DependentCostsWithoutTax,
    decimal Total,
    decimal CurrencyPrice,
    decimal CurrencyTotal,
    string UnitOfMeasureId,
    int Packaging,
    bool CalculateExcise,
    bool CalculateTax,
    int? TaxationMethodId,
    int? StatusId,
    decimal Weight,
    string? Description,
    double ProductionQuantity,
    string? ProductionUnitOfMeasureId,
    double ProductionQuantityCoefficient,
    int? MealOrderLineId,
    int? MealTypeId,
    bool CalculateAuxiliaryTax,
    int? BaseAccountId,
    decimal ActionDiscount,
    bool? DeliveryOfGoods,
    decimal Discount2,
    decimal? LastPurchasePrice,
    decimal? AveragePrice,
    int? CurrencyDays,
    DateTime? CurrencyDate,
    decimal? PriceWithoutTax,
    string? MandatoryEquipment,
    string? SupplementaryEquipment,
    decimal? AveragePriceUJ,
    decimal? ReturnAmount,
    decimal? OldPrice,
    int? ColorId,
    string ETag,  // Base64(RowVersion) - KRITIČNO!
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int? CreatedBy,
    int? UpdatedBy
);
```

---

## 🔐 STORED PROCEDURE SERVICE - SVE 11 SP-a

```csharp
public class StoredProcedureService : IStoredProcedureService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StoredProcedureService> _logger;

    public StoredProcedureService(AppDbContext context, ILogger<StoredProcedureService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 1. spPartnerComboStatusNabavka
    public async Task<List<PartnerComboDto>> GetPartnerComboAsync()
    {
        _logger.LogInformation("Executing spPartnerComboStatusNabavka");
        return await _context.Database
            .SqlQueryRaw<PartnerComboDto>("EXECUTE spPartnerComboStatusNabavka")
            .ToListAsync();
    }

    // 2. spOrganizacionaJedinicaCombo
    public async Task<List<OrgUnitComboDto>> GetOrgUnitsComboAsync(string? docTypeId)
    {
        _logger.LogInformation("Executing spOrganizacionaJedinicaCombo with docType={DocType}", docTypeId);
        return await _context.Database
            .SqlQueryRaw<OrgUnitComboDto>(
                "EXECUTE spOrganizacionaJedinicaCombo @IDVrstaDokumenta = {0}",
                docTypeId ?? "")
            .ToListAsync();
    }

    // 3. spNacinOporezivanjaComboNabavka
    public async Task<List<TaxationMethodComboDto>> GetTaxationMethodsComboAsync()
    {
        _logger.LogInformation("Executing spNacinOporezivanjaComboNabavka");
        return await _context.Database
            .SqlQueryRaw<TaxationMethodComboDto>("EXECUTE spNacinOporezivanjaComboNabavka")
            .ToListAsync();
    }

    // 4. spReferentCombo
    public async Task<List<ReferentComboDto>> GetReferentComboAsync()
    {
        _logger.LogInformation("Executing spReferentCombo");
        return await _context.Database
            .SqlQueryRaw<ReferentComboDto>("EXECUTE spReferentCombo")
            .ToListAsync();
    }

    // 5. spDokumentNDCombo
    public async Task<List<DocumentNDComboDto>> GetDocumentNDComboAsync()
    {
        _logger.LogInformation("Executing spDokumentNDCombo");
        return await _context.Database
            .SqlQueryRaw<DocumentNDComboDto>("EXECUTE spDokumentNDCombo")
            .ToListAsync();
    }

    // 6. spPoreskaStopaCombo
    public async Task<List<TaxRateComboDto>> GetTaxRateComboAsync()
    {
        _logger.LogInformation("Executing spPoreskaStopaCombo");
        return await _context.Database
            .SqlQueryRaw<TaxRateComboDto>("EXECUTE spPoreskaStopaCombo")
            .ToListAsync();
    }

    // 7. spArtikalComboUlaz
    public async Task<List<ArticleComboDto>> GetArticleComboAsync()
    {
        _logger.LogInformation("Executing spArtikalComboUlaz");
        return await _context.Database
            .SqlQueryRaw<ArticleComboDto>("EXECUTE spArtikalComboUlaz")
            .ToListAsync();
    }

    // 8. spDokumentTroskoviLista
    public async Task<List<CostListDto>> GetDocumentCostsListAsync(int documentId)
    {
        _logger.LogInformation("Executing spDokumentTroskoviLista for documentId={DocumentId}", documentId);
        return await _context.Database
            .SqlQueryRaw<CostListDto>(
                "EXECUTE spDokumentTroskoviLista @IDDokument = {0}",
                documentId)
            .ToListAsync();
    }

    // 9. spUlazniRacuniIzvedeniTroskoviCombo
    public async Task<List<CostTypeComboDto>> GetCostTypesComboAsync()
    {
        _logger.LogInformation("Executing spUlazniRacuniIzvedeniTroskoviCombo");
        return await _context.Database
            .SqlQueryRaw<CostTypeComboDto>("EXECUTE spUlazniRacuniIzvedeniTroskoviCombo")
            .ToListAsync();
    }

    // 10. spNacinDeljenjaTroskovaCombo
    public async Task<List<CostDistributionComboDto>> GetCostDistributionMethodsComboAsync()
    {
        _logger.LogInformation("Executing spNacinDeljenjaTroskovaCombo");
        return await _context.Database
            .SqlQueryRaw<CostDistributionComboDto>("EXECUTE spNacinDeljenjaTroskovaCombo")
            .ToListAsync();
    }

    // 11. spDokumentTroskoviArtikliCOMBO
    public async Task<List<CostArticleComboDto>> GetDocumentCostArticlesComboAsync(int documentId)
    {
        _logger.LogInformation("Executing spDokumentTroskoviArtikliCOMBO for documentId={DocumentId}", documentId);
        return await _context.Database
            .SqlQueryRaw<CostArticleComboDto>(
                "EXECUTE spDokumentTroskoviArtikliCOMBO @IDDokument = {0}",
                documentId)
            .ToListAsync();
    }
}
```

---

## 🔌 CONTROLLERS - Sa ETag Konkurentnosti

```csharp
[ApiController]
[Route("api/v1/documents/{documentId:int}/items")]
[Authorize]
public class DocumentItemsController : ControllerBase
{
    private readonly IDocumentLineItemService _service;
    private readonly ILogger<DocumentItemsController> _logger;

    [HttpPost]
    public async Task<ActionResult<DocumentLineItemDto>> CreateItem(
        int documentId,
        [FromBody] CreateLineItemDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(documentId, dto);
            return CreatedAtAction(nameof(GetItem), new { documentId, itemId = result.Id }, result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ProblemDetailsDto(400, "Validacija", ex.Message));
        }
    }

    /// KRITIČNO: Vraća ETag header!
    [HttpGet("{itemId:int}")]
    public async Task<ActionResult<DocumentLineItemDto>> GetItem(int documentId, int itemId)
    {
        var result = await _service.GetAsync(documentId, itemId);
        if (result == null) return NotFound();

        Response.Headers.ETag = $"\"{result.ETag}\"";
        return Ok(result);
    }

    /// KRITIČNO: PATCH sa If-Match header-om (konkurentnost!)
    [HttpPatch("{itemId:int}")]
    public async Task<ActionResult<DocumentLineItemDto>> UpdateItem(
        int documentId,
        int itemId,
        [FromBody] PatchLineItemDto dto)
    {
        var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
        if (string.IsNullOrEmpty(ifMatch))
            return BadRequest("Missing If-Match header");

        byte[] expectedRowVersion;
        try
        {
            expectedRowVersion = Convert.FromBase64String(ifMatch.Trim('\"'));
        }
        catch
        {
            return BadRequest("Invalid ETag format");
        }

        try
        {
            var result = await _service.UpdateAsync(documentId, itemId, expectedRowVersion, dto);
            Response.Headers.ETag = $"\"{result.ETag}\"";
            return Ok(result);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning("Konflikt: {Message}", ex.Message);
            return Conflict(new { message = "Stavka promenjena" });
        }
    }

    [HttpDelete("{itemId:int}")]
    public async Task<ActionResult> DeleteItem(int documentId, int itemId)
    {
        await _service.DeleteAsync(documentId, itemId);
        return NoContent();
    }
}
```

---

## 🎯 SERVICE - Sa Konkurentnosti (Iz v2)

```csharp
public class DocumentLineItemService : IDocumentLineItemService
{
    private readonly IDocumentLineItemRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<PatchLineItemDto> _validator;
    private readonly ILogger<DocumentLineItemService> _logger;

    public async Task<DocumentLineItemDto> UpdateAsync(
        int documentId,
        int itemId,
        byte[] expectedRowVersion,  // ETag iz If-Match
        PatchLineItemDto dto)
    {
        // 1. Validacija
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. Učitaj stavku
        var item = await _repository.GetByIdAsync(itemId);
        if (item == null || item.DocumentId != documentId)
            throw new NotFoundException("Stavka nije pronađena");

        // 3. KONKURENTNOST PROVERA - KRITIČNO!
        if (!item.RowVersion.SequenceEqual(expectedRowVersion))
        {
            _logger.LogWarning("Konflikt: RowVersion mismatch za item {ItemId}", itemId);
            throw new ConflictException("Stavka je promenjena od drugog korisnika");
        }

        // 4. Update
        _mapper.Map(dto, item);
        item.UpdatedAt = DateTime.UtcNow;

        _repository.Update(item);
        await _repository.SaveChangesAsync();

        return _mapper.Map<DocumentLineItemDto>(item);
    }
}
```

---

## ✅ FINAL CHECKLIST v3.1

- ✅ **65 svojstava DocumentLineItem** (svi iz baze)
- ✅ **86 svojstava Document** (svi iz baze)
- ✅ **ID-evi su int** (kako je u bazi!)
- ✅ **RowVersion konkurentnost** (tblStavkaDokumenta.StavkaDokumentaTimeStamp)
- ✅ **ETag sa If-Match** (PATCH kontrola)
- ✅ **Sve 11 SP-a** u StoredProcedureService
- ✅ **DTOs sa tri forme** (Create/Patch/Response)
- ✅ **Controllers sa ETag** (sve iz v2)
- ✅ **Services sa konkurentnosti** (sve iz v2)
- ✅ **Clean Architecture** (4 sloja - sve iz v2)

---

**v3.1 - FINALNO OBJEDINJENO - SVE JE OVDE!** ✅