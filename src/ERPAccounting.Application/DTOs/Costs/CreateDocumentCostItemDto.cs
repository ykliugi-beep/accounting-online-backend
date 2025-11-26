namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za kreiranje stavke zavisnog troška
/// Mapira se na tblDokumentTroskoviStavka.
/// PDV stavke se prosleđuju kao lista (VatItems) i upisuju u tblDokumentTroskoviStavkaPDV.
/// </summary>
public record CreateDocumentCostItemDto(
    /// <summary>IDUlazniRacuniIzvedeni - Vrsta troška (Transport, Carina, Osiguranje...)</summary>
    int CostTypeId,
    
    /// <summary>IDNacinDeljenjaTroskova - Način raspodele (1=Po količini, 2=Po vrednosti, 3=Ručno)</summary>
    int DistributionMethodId,
    
    /// <summary>Iznos - Osnovni iznos stavke troška (bez PDV)</summary>
    decimal Amount,
    
    /// <summary>SveStavke - Da li se primenjuje na sve stavke dokumenta (default true)</summary>
    bool ApplyToAllItems,
    
    /// <summary>IDStatus - Status stavke</summary>
    int StatusId,
    
    /// <summary>ObracunPorezTroskovi - Da li se računa porez na troškove (0/1)</summary>
    bool CalculateTaxOnCost,
    
    /// <summary>DodajPDVNaTroskove - Da li se dodaje PDV na troškove (0/1)</summary>
    bool AddVatToCost,
    
    /// <summary>IznosValuta - Iznos u devizi (opciono)</summary>
    decimal? CurrencyAmount,
    
    /// <summary>Gotovina - Iznos plaćen gotovinom</summary>
    decimal? CashAmount,
    
    /// <summary>Kartica - Iznos plaćen karticom</summary>
    decimal? CardAmount,
    
    /// <summary>Virman - Iznos plaćen virmanom</summary>
    decimal? WireTransferAmount,
    
    /// <summary>Kolicina - Količina (opciono, za raspodelu po količini)</summary>
    decimal? Quantity,
    
    /// <summary>Lista PDV stavki (mapira se na tblDokumentTroskoviStavkaPDV)</summary>
    List<CostItemVatDto> VatItems
);

/// <summary>
/// DTO za PDV stavku na trošku
/// Mapira se na tblDokumentTroskoviStavkaPDV
/// </summary>
public record CostItemVatDto(
    /// <summary>IDPoreskaStopa - Poreska stopa (char(2), npr. "01", "02")</summary>
    string TaxRateId,
    
    /// <summary>IznosPDV - Iznos PDV po ovoj stopi</summary>
    decimal VatAmount
);