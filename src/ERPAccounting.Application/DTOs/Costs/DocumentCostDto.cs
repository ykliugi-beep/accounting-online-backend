namespace ERPAccounting.Application.DTOs.Costs;

/// <summary>
/// DTO za prikaz zavisnog troška (zaglavlje)
/// Mapira se na tblDokumentTroskovi sa izračunatim ukupnim iznosima iz stavki.
/// </summary>
public record DocumentCostDto(
    /// <summary>IDDokumentTroskovi - Jedinstveni ID troška</summary>
    int Id,
    
    /// <summary>IDDokument - ID glavnog dokumenta</summary>
    int DocumentId,
    
    /// <summary>IDPartner - ID partnera (dobavljača)</summary>
    int PartnerId,
    
    /// <summary>Naziv partnera (join sa tblPartner)</summary>
    string PartnerName,
    
    /// <summary>IDVrstaDokumenta - Vrsta troška (char(2))</summary>
    string DocumentTypeCode,
    
    /// <summary>BrojDokumenta - Broj dokumenta troška</summary>
    string DocumentNumber,
    
    /// <summary>DatumDPO - Datum primanja obveze</summary>
    DateTime DueDate,
    
    /// <summary>DatumValute - Datum valute</summary>
    DateTime? CurrencyDate,
    
    /// <summary>Opis - Opis troška</summary>
    string? Description,
    
    /// <summary>IDStatus - Status troška</summary>
    int StatusId,
    
    /// <summary>IDValuta - Valuta</summary>
    int? CurrencyId,
    
    /// <summary>Kurs - Kurs valute</summary>
    decimal? ExchangeRate,
    
    /// <summary>Ukupan iznos bez PDV (suma svih stavki)</summary>
    /// <remarks>Izračunava se: SUM(tblDokumentTroskoviStavka.Iznos)</remarks>
    decimal TotalAmountNet,
    
    /// <summary>Ukupan iznos PDV (suma PDV iz svih stavki)</summary>
    /// <remarks>Izračunava se: SUM(tblDokumentTroskoviStavkaPDV.IznosPDV)</remarks>
    decimal TotalAmountVat,
    
    /// <summary>Lista stavki troška</summary>
    List<DocumentCostItemDto> Items,
    
    /// <summary>ETag za konkurentnost (Base64 RowVersion)</summary>
    string ETag
);