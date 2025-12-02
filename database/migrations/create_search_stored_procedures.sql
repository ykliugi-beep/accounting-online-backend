-- =============================================
-- Author:      AI Assistant
-- Create date: 2025-01-03
-- Description: Stored Procedures za server-side search
-- =============================================

-- ════════════════════════════════════════════════
-- 1. spPartnerSearch - Pretraga partnera
-- ════════════════════════════════════════════════

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[spPartnerSearch]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[spPartnerSearch]
END
GO

CREATE PROCEDURE [dbo].[spPartnerSearch]
    @SearchTerm NVARCHAR(100),
    @Limit INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limit)
        PartnerID AS IdPartner,
        Naziv AS NazivPartnera,
        Mesto,
        Opis,
        StatusID AS IdStatus,
        NacinOporezivanjaID_Nabavka AS IdNacinOporezivanjaNabavka,
        ObracunAkciza,
        ObracunPorez,
        ReferentID AS IdReferent,
        Sifra AS SifraPartner
    FROM tblPartner
    WHERE StatusNabavka = 'Aktivan'
      AND (Sifra LIKE '%' + @SearchTerm + '%' OR Naziv LIKE '%' + @SearchTerm + '%')
    ORDER BY Naziv
END
GO

-- Test
-- EXEC spPartnerSearch @SearchTerm = 'sim', @Limit = 10

-- ════════════════════════════════════════════════
-- 2. spArticleSearch - Pretraga artikala
-- ════════════════════════════════════════════════

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[spArticleSearch]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[spArticleSearch]
END
GO

CREATE PROCEDURE [dbo].[spArticleSearch]
    @SearchTerm NVARCHAR(100),
    @Limit INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limit)
        ArtikalID AS IdArtikal,
        Sifra AS SifraArtikal,
        Naziv AS NazivArtikla,
        JedinicaMere,
        PoreskaStopaID AS IdPoreskaStopa,
        ProcenatPoreza,
        Akciza,
        KoeficijentKolicine,
        ImaLot,
        OtkupnaCena,
        PoljoprivredniProizvod
    FROM tblArtikal
    WHERE StatusUlaz = 'Aktivan'
      AND (Sifra LIKE '%' + @SearchTerm + '%' OR Naziv LIKE '%' + @SearchTerm + '%')
    ORDER BY Naziv
END
GO

-- Test
-- EXEC spArticleSearch @SearchTerm = 'crna', @Limit = 10

-- ════════════════════════════════════════════════
-- DONE!
-- ════════════════════════════════════════════════
PRINT 'Stored procedures created successfully!'
PRINT ''
PRINT 'Test commands:'
PRINT 'EXEC spPartnerSearch @SearchTerm = ''sim'', @Limit = 10'
PRINT 'EXEC spArticleSearch @SearchTerm = ''crna'', @Limit = 10'
