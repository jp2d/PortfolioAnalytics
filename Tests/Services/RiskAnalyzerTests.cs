using Microsoft.Extensions.Logging.Abstractions;
using PortfolioAnalytics.Models;
using PortfolioAnalytics.Service;
using Tests.Helpers;

namespace Tests.Services
{
    public class RiskAnalyzerTests
    {
        private static RiskAnalyzer CreateSut(FakePerformanceCalculator fake) =>
            new(fake, NullLogger<RiskAnalyzer>.Instance);

        private static MarketData MakeMarketData(decimal selicRate) => new() { SelicRate = selicRate };

        [Fact]
        public void SharpeRatio_CenarioPositivo_CalculaCorretamente()
        {
            var fake = new FakePerformanceCalculator
            {
                Result = new PerformanceMetrics
                {
                    AnnualizedReturnPercent = 20m, // 20% a.a.
                    VolatilityPercent = 10m,       // 10% a.a.
                    Positions = new List<PositionPerformanceMetric>
                {
                    new() { Symbol = "AAA", WeightPercent = 100m, CurrentValue = 1000m }
                }
                }
            };
            var sut = CreateSut(fake);

            var result = sut.Analyze(new Portfolio { Id = "p1" }, new Dictionary<string, Asset>(), MakeMarketData(0.10m));

            // Sharpe = (20 - 10) / 10 = 1.0
            Assert.NotNull(result.SharpeRatio);
            Assert.Equal(1.0m, result.SharpeRatio!.Value, precision: 4);
        }

        [Fact]
        public void SharpeRatio_CenarioNegativo_CalculaCorretamente()
        {
            var fake = new FakePerformanceCalculator
            {
                Result = new PerformanceMetrics
                {
                    AnnualizedReturnPercent = -20m,
                    VolatilityPercent = 15m,
                    Positions = new List<PositionPerformanceMetric>
                {
                    new() { Symbol = "AAA", WeightPercent = 100m, CurrentValue = 1000m }
                }
                }
            };
            var sut = CreateSut(fake);

            var result = sut.Analyze(new Portfolio { Id = "p1" }, new Dictionary<string, Asset>(), MakeMarketData(0.12m));

            // Sharpe = (-20 - 12) / 15 = -2.1333...
            Assert.NotNull(result.SharpeRatio);
            Assert.Equal(-2.1333m, result.SharpeRatio!.Value, precision: 3);
        }

        [Fact]
        public void SharpeRatio_VolatilidadeZero_RetornaNull()
        {
            var fake = new FakePerformanceCalculator
            {
                Result = new PerformanceMetrics
                {
                    AnnualizedReturnPercent = 20m,
                    VolatilityPercent = 0m,
                    Positions = new List<PositionPerformanceMetric>()
                }
            };
            var sut = CreateSut(fake);

            var result = sut.Analyze(new Portfolio { Id = "p1" }, new Dictionary<string, Asset>(), MakeMarketData(0.10m));

            Assert.Null(result.SharpeRatio);
        }

        [Fact]
        public void ConcentrationRisk_IdentificaMaiorPosicaoETop3()
        {
            var fake = new FakePerformanceCalculator
            {
                Result = new PerformanceMetrics
                {
                    AnnualizedReturnPercent = 10m,
                    VolatilityPercent = 10m,
                    Positions = new List<PositionPerformanceMetric>
                {
                    new() { Symbol = "AAA", WeightPercent = 40m, CurrentValue = 4000m },
                    new() { Symbol = "BBB", WeightPercent = 30m, CurrentValue = 3000m },
                    new() { Symbol = "CCC", WeightPercent = 20m, CurrentValue = 2000m },
                    new() { Symbol = "DDD", WeightPercent = 10m, CurrentValue = 1000m }
                }
                }
            };
            var assetsBySymbol = new Dictionary<string, Asset>
            {
                ["AAA"] = new Asset { Symbol = "AAA", Sector = "S1" },
                ["BBB"] = new Asset { Symbol = "BBB", Sector = "S2" },
                ["CCC"] = new Asset { Symbol = "CCC", Sector = "S3" },
                ["DDD"] = new Asset { Symbol = "DDD", Sector = "S4" }
            };
            var sut = CreateSut(fake);

            var result = sut.Analyze(new Portfolio { Id = "p1" }, assetsBySymbol, MakeMarketData(0.10m));

            Assert.Equal("AAA", result.LargestPosition.Symbol);
            Assert.Equal(40m, result.LargestPosition.Percentage);
            Assert.Equal(90m, result.Top3ConcentrationPercent); // 40+30+20
        }

        [Theory]
        [InlineData(14.99, "Low")]
        [InlineData(15.0, "Medium")]
        [InlineData(25.0, "Medium")]  // 25% exato ainda é Médio
        [InlineData(25.01, "High")]
        public void RiskLevel_PosicaoNasFronteiras_ClassificaConformeDocumentado(decimal weight, string expectedRisk)
        {
            var fake = new FakePerformanceCalculator
            {
                Result = new PerformanceMetrics
                {
                    AnnualizedReturnPercent = 10m,
                    VolatilityPercent = 10m,
                    Positions = new List<PositionPerformanceMetric>
                {
                    new() { Symbol = "AAA", WeightPercent = weight, CurrentValue = weight * 100m }
                }
                }
            };
            var assetsBySymbol = new Dictionary<string, Asset> { ["AAA"] = new Asset { Symbol = "AAA", Sector = "S1" } };
            var sut = CreateSut(fake);

            var result = sut.Analyze(new Portfolio { Id = "p1" }, assetsBySymbol, MakeMarketData(0.10m));

            Assert.Equal(expectedRisk, result.OverallRisk.ToString());
        }

        [Theory]
        [InlineData(24.99, "Low")]     // abaixo de 25% -> Baixo
        [InlineData(25.0, "Medium")]   // 25% exato -> Médio (fronteira inferior)
        [InlineData(40.0, "Medium")]   // 40% exato -> ainda Médio (fronteira superior)
        [InlineData(40.01, "High")]    // acima de 40% -> Alto
        public void RiskLevel_SetorNasFronteiras_ClassificaConformeDocumentado(decimal sectorWeight, string expectedRisk)
        {
            // Duas posições no mesmo setor, cada uma abaixo do limiar de posição (15%-25%),
            // isolando a classificação por SETOR da classificação por posição individual.
            var half = sectorWeight / 2m;
            var fake = new FakePerformanceCalculator
            {
                Result = new PerformanceMetrics
                {
                    AnnualizedReturnPercent = 10m,
                    VolatilityPercent = 10m,
                    Positions = new List<PositionPerformanceMetric>
            {
                new() { Symbol = "AAA", WeightPercent = half, CurrentValue = half * 100m },
                new() { Symbol = "BBB", WeightPercent = half, CurrentValue = half * 100m }
            }
                }
            };
            var assetsBySymbol = new Dictionary<string, Asset>
            {
                ["AAA"] = new Asset { Symbol = "AAA", Sector = "MesmoSetor" },
                ["BBB"] = new Asset { Symbol = "BBB", Sector = "MesmoSetor" }
            };
            var sut = CreateSut(fake);

            var result = sut.Analyze(new Portfolio { Id = "p1" }, assetsBySymbol, MakeMarketData(0.10m));

            var sector = result.SectorDiversification.Single();
            Assert.Equal(expectedRisk, sector.Risk.ToString());
        }
    }
}
