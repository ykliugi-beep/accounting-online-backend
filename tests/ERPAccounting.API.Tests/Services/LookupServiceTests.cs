using System.Collections.Generic;
using System.Threading.Tasks;
using ERPAccounting.Application.Services;
using ERPAccounting.Domain.Abstractions.Gateways;
using ERPAccounting.Domain.Lookups;
using Moq;
using Xunit;

namespace ERPAccounting.API.Tests.Services;

public class LookupServiceTests
{
    [Fact]
    public async Task GetArticlesComboAsync_ReturnsValuesWithoutCastingIssues()
    {
        var gatewayMock = new Mock<IStoredProcedureGateway>();
        var lookup = new ArticleLookup(
            IdArtikal: 10,
            SifraArtikal: "A-001",
            NazivArtikla: "Test Artikal",
            JedinicaMere: "KG",
            IdPoreskaStopa: "20",
            ProcenatPoreza: 20.5,
            Akciza: 12.3456m,
            KoeficijentKolicine: 1.2345m,
            ImaLot: true,
            OtkupnaCena: 999.8888m,
            PoljoprivredniProizvod: false);

        gatewayMock.Setup(g => g.GetArticlesComboAsync())
            .ReturnsAsync(new List<ArticleLookup> { lookup });

        var service = new LookupService(gatewayMock.Object);

        var result = await service.GetArticlesComboAsync();

        var article = Assert.Single(result);
        Assert.Equal(lookup.Akciza, article.Akciza);
        Assert.Equal(lookup.KoeficijentKolicine, article.KoeficijentKolicine);
        Assert.Equal(lookup.OtkupnaCena, article.OtkupnaCena);
        Assert.Equal(lookup.ProcenatPoreza, article.ProcenatPoreza);
    }
}
