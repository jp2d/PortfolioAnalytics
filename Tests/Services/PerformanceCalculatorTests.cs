using Microsoft.Extensions.Logging.Abstractions;
using PortfolioAnalytics.Models;
using PortfolioAnalytics.Service;
using Tests.Helpers;

namespace Tests.Services
{
    public class PerformanceCalculatorTests
    {
        private static PerformanceCalculator CreateSut(DateTime now) =>
            new(new FakeClock(now), NullLogger<PerformanceCalculator>.Instance);

        private static Asset MakeAsset(string symbol, decimal currentPrice, List<PricePoint>? history = null) => new()
        {
            Symbol = symbol,
            Name = symbol,
            Type = "Stock",
            Sector = "TestSector",
            CurrentPrice = currentPrice,
            PriceHistory = history ?? new List<PricePoint>()
        };

        private static Portfolio MakePortfolio(decimal totalInvestment, DateTime? createdAt, params Position[] positions) => new()
        {
            Id = "test-portfolio",
            Name = "Test",
            UserId = "test-user",
            TotalInvestment = totalInvestment,
            CreatedAt = createdAt,
            Positions = positions.ToList()
        };

        [Fact]
        public void TotalReturn_ComValoresValidos_CalculaCorretamente()
        {
            // Investido 5000, atual 6000 => +20%
            var asset = MakeAsset("XPTO", currentPrice: 60m);
            var position = new Position { AssetSymbol = "XPTO", Quantity = 100, AveragePrice = 50m, TargetAllocation = 1m };
            var portfolio = MakePortfolio(totalInvestment: 5000m, createdAt: null, position);
            var sut = CreateSut(new DateTime(2024, 10, 6));

            var result = sut.Calculate(portfolio, new Dictionary<string, Asset> { ["XPTO"] = asset });

            Assert.Equal(6000m, result.CurrentValue);
            Assert.NotNull(result.TotalReturnPercent);
            Assert.Equal(20m, result.TotalReturnPercent!.Value, precision: 4);
        }

        [Fact]
        public void TotalReturn_InvestimentoZero_RetornaNull()
        {
            var asset = MakeAsset("XPTO", currentPrice: 60m);
            var position = new Position { AssetSymbol = "XPTO", Quantity = 100, AveragePrice = 50m, TargetAllocation = 1m };
            var portfolio = MakePortfolio(totalInvestment: 0m, createdAt: null, position);
            var sut = CreateSut(new DateTime(2024, 10, 6));

            var result = sut.Calculate(portfolio, new Dictionary<string, Asset> { ["XPTO"] = asset });

            Assert.Null(result.TotalReturnPercent);
            Assert.Null(result.AnnualizedReturnPercent);
        }

        [Fact]
        public void Volatility_ComHistoricoConhecido_RetornaDesvioAnualizado()
        {
            // Série artificial com retornos exatos: +10%, -10%, +10%, -10%
            // stdDev amostral = 0.11547005..., anualizado (x sqrt(252)) ≈ 183.3%
            var history = new List<PricePoint>
        {
            new() { Date = new DateOnly(2024, 9, 1), Price = 100m },
            new() { Date = new DateOnly(2024, 9, 2), Price = 110m },
            new() { Date = new DateOnly(2024, 9, 3), Price = 99m },
            new() { Date = new DateOnly(2024, 9, 4), Price = 108.9m },
            new() { Date = new DateOnly(2024, 9, 5), Price = 98.01m }
        };
            var asset = MakeAsset("XPTO", currentPrice: 100m, history);
            var position = new Position { AssetSymbol = "XPTO", Quantity = 10, AveragePrice = 100m, TargetAllocation = 1m };
            var portfolio = MakePortfolio(totalInvestment: 1000m, createdAt: null, position);
            var sut = CreateSut(new DateTime(2024, 10, 6));

            var result = sut.Calculate(portfolio, new Dictionary<string, Asset> { ["XPTO"] = asset });

            Assert.NotNull(result.VolatilityPercent);
            Assert.Equal(183.3m, result.VolatilityPercent!.Value, precision: 1);
        }

        [Fact]
        public void Volatility_SemHistorico_RetornaNull()
        {
            var asset = MakeAsset("XPTO", currentPrice: 60m); // sem histórico
            var position = new Position { AssetSymbol = "XPTO", Quantity = 100, AveragePrice = 50m, TargetAllocation = 1m };
            var portfolio = MakePortfolio(totalInvestment: 5000m, createdAt: null, position);
            var sut = CreateSut(new DateTime(2024, 10, 6));

            var result = sut.Calculate(portfolio, new Dictionary<string, Asset> { ["XPTO"] = asset });

            Assert.Null(result.VolatilityPercent);
        }
    }
}
