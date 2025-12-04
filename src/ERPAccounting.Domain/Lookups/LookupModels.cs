using System.ComponentModel.DataAnnotations.Schema;

namespace ERPAccounting.Domain.Lookups;

// SP 1: spPartnerComboStatusNabavka
// Returns: NAZIV PARTNERA, MESTO, IDPartner, Opis, IDStatus, 
//          IDNacinOporezivanjaNabavka, ObracunAkciza, ObracunPorez, IDReferent, ŠIFRA
public record PartnerLookup(
    [property: Column("IDPartner")] int IdPartner,
    [property: Column("NAZIV PARTNERA")] string NazivPartnera,
    [property: Column("MESTO")] string? Mesto,
    [property: Column("Opis")] string? Opis,
    [property: Column("IDStatus")] int IdStatus,
    [property: Column("IDNacinOporezivanjaNabavka")] int? IdNacinOporezivanjaNabavka,
    [property: Column("ObracunAkciza")] short ObracunAkciza,
    [property: Column("ObracunPorez")] short ObracunPorez,
    [property: Column("IDReferent")] int? IdReferent,
    [property: Column("ŠIFRA")] string? SifraPartner
);

// SP 2: spOrganizacionaJedinicaCombo
// Returns: IDOrganizacionaJedinica, NAZIV MAGACINA, MESTO, SifraOrganizacionaJedinica
public record OrgUnitLookup(
    [property: Column("IDOrganizacionaJedinica")] int IdOrganizacionaJedinica,
    [property: Column("NAZIV MAGACINA")] string Naziv,
    [property: Column("MESTO")] string? Mesto,
    [property: Column("SifraOrganizacionaJedinica")] string? Sifra
);

// SP 3: spNacinOporezivanjaComboNabavka
// Returns: IDNacinOporezivanja, Opis, ObracunAkciza, ObracunPorez, ObracunPorezPomocni
public record TaxationMethodLookup(
    [property: Column("IDNacinOporezivanja")] int IdNacinOporezivanja,
    [property: Column("Opis")] string Opis,
    [property: Column("ObracunAkciza")] short ObracunAkciza,
    [property: Column("ObracunPorez")] short ObracunPorez,
    [property: Column("ObracunPorezPomocni")] short ObracunPorezPomocni
);

// SP 4: spReferentCombo
// Returns: IDRadnik, ImeRadnika as [IME I PREZIME], SifraRadnika
public record ReferentLookup(
    [property: Column("IDRadnik")] int IdRadnik,
    [property: Column("IME I PREZIME")] string ImePrezime,
    [property: Column("SifraRadnika")] string? SifraRadnika
);

// SP 5: spDokumentNDCombo
// Returns: IDDokument, BrojDokumenta, Datum, NazivPartnera
public record DocumentNDLookup(
    [property: Column("IDDokument")] int IdDokument,
    [property: Column("BrojDokumenta")] string BrojDokumenta,
    [property: Column("Datum")] DateTime Datum,
    [property: Column("NazivPartnera")] string NazivPartnera
);

// SP 6: spPoreskaStopaCombo
// Returns: IDPoreskaStopa, Naziv (ONLY 2 columns!)
// NOTE: ProcenatPoreza is NOT returned by this SP, only by spArtikalComboUlaz
public record TaxRateLookup(
    [property: Column("IDPoreskaStopa")] string IdPoreskaStopa,
    [property: Column("Naziv")] string Naziv
    // ProcenatPoreza - REMOVED: Not in SP output
);

// SP 7: spArtikalComboUlaz
// Returns: IDArtikal, SIFRA, NAZIV ARTIKLA, JM, IDPoreskaStopa, ProcenatPoreza,
//          Akciza, KoeficijentKolicine, ImaLot, OtkupnaCena, PoljoprivredniProizvod
public record ArticleLookup(
    [property: Column("IDArtikal")] int IdArtikal,
    [property: Column("SIFRA")] string SifraArtikal,
    [property: Column("NAZIV ARTIKLA")] string NazivArtikla,
    [property: Column("JM")] string? JedinicaMere,
    [property: Column("IDPoreskaStopa")] string? IdPoreskaStopa,
    [property: Column("ProcenatPoreza")] double ProcenatPoreza,  // Available HERE from JOIN
    [property: Column("Akciza", TypeName = "money")] decimal Akciza,
    [property: Column("KoeficijentKolicine", TypeName = "money")] decimal KoeficijentKolicine,
    [property: Column("ImaLot")] bool ImaLot,
    [property: Column("OtkupnaCena", TypeName = "money")] decimal? OtkupnaCena,
    [property: Column("PoljoprivredniProizvod")] bool PoljoprivredniProizvod
);

// SP 8: spDokumentTroskoviLista
// Returns: IDDokumentTroskovi, IDDokumentTroskoviStavka, LISTA ZAVISNIH TROSKOVA, OSNOVICA, PDV
public record DocumentCostLookup(
    [property: Column("IDDokumentTroskovi")] int IdDokumentTroskovi,
    [property: Column("IDDokumentTroskoviStavka")] int? IdDokumentTroskoviStavka,
    [property: Column("ListaTroskova")] string ListaTroskova,
    [property: Column("OSNOVICA")] decimal? Osnovica,
    [property: Column("PDV")] decimal? Pdv
);

// SP 9: spUlazniRacuniIzvedeniTroskoviCombo
// Returns: IDUlazniRacuniIzvedeni, Naziv, Opis, NazivSpecifikacije, ObracunPorez, IDUlazniRacuniOsnovni
public record CostTypeLookup(
    [property: Column("IDUlazniRacuniIzvedeni")] int IdUlazniRacuniIzvedeni,
    [property: Column("Naziv")] string Naziv,
    [property: Column("Opis")] string? Opis,
    [property: Column("NazivSpecifikacije")] string? NazivSpecifikacije,
    [property: Column("ObracunPorez")] int ObracunPorez,
    [property: Column("IDUlazniRacuniOsnovni")] int IdUlazniRacuniOsnovni
);

// SP 10: spNacinDeljenjaTroskovaCombo
// Returns: IDNacinDeljenjaTroskova, Naziv, OpisNacina
public record CostDistributionMethodLookup
{
    [Column("IDNacinDeljenjaTroskova")]
    public short IdNacinDeljenjaTroskova { get; set; }

    [Column("Naziv")]
    public string Naziv { get; set; } = string.Empty;

    [Column("OpisNacina")]
    public string OpisNacina { get; set; } = string.Empty;
}

// SP 11: spDokumentTroskoviArtikliCOMBO
// Returns: IDStavkaDokumenta, SifraArtikal, NazivArtikla
public record CostArticleLookup(
    [property: Column("IDStavkaDokumenta")] int IdStavkaDokumenta,
    [property: Column("SifraArtikal")] string SifraArtikal,
    [property: Column("NazivArtikla")] string NazivArtikla
);