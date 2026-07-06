using Microsoft.Extensions.Logging.Abstractions;
using PortfolioAnalytics.Models;
using PortfolioAnalytics.Models.Enum;
using PortfolioAnalytics.Service;
using Tests.Helpers;

namespace Tests.Services
{
    public class RebalancingOptimizerTests
    {
        private static RebalancingOptimizer CreateSut(FakePerformanceCalculator fake) =>
            new(fake, NullLogger<RebalancingOptimizer>.Instance);

        private static Portfolio MakePortfolio(params (string Symbol, decimal Target)[] targets) => new()
        {
            Id = "p1",
            Positions = targets.Select(t => new Position { AssetSymbol = t.Symbol, TargetAllocation = t.Target }).ToList()
        };

        private static PerformanceMetrics MakePerformance(decimal totalValue, params (string Symbol, decimal Weight, decimal Value)[] positions) => new()
        {
            CurrentValue = totalValue,
            Positions = positions.Select(p => new PositionPerformanceMetric
            {
                Symbol = p.Symbol,
                WeightPercent = p.Weight,
                CurrentValue = p.Value
            }).ToList()
        };

        [Fact]
        public void Rebalancing_DesvioAcimaDe2_GeraTradeComQuantidadeECustoCorretos()
        {
            var fake = new FakePerformanceCalculator
            {
                Result = MakePerformance(10000m, ("AAA", 30m, 3000m), ("BBB", 70m, 7000m))
            };
            var portfolio = MakePortfolio(("AAA", 0.20m), ("BBB", 0.80m));
            var assetsBySymbol = new Dictionary<string, Asset>
            {
                ["AAA"] = new Asset { Symbol = "AAA", CurrentPrice = 10m },
                ["BBB"] = new Asset { Symbol = "BBB", CurrentPrice = 50m }
            };
            var sut = CreateSut(fake);

            var result = sut.Optimize(portfolio, assetsBySymbol);

            var aaaTrade = result.SuggestedTrades.Single(t => t.Symbol == "AAA");
            Assert.Equal(TradeAction.Sell, aaaTrade.Action);
            Assert.Equal(100, aaaTrade.Quantity);
            Assert.Equal(1000m, aaaTrade.EstimatedValue);
            Assert.Equal(3.0m, aaaTrade.TransactionCost, precision: 4);
            Assert.True(result.NeedsRebalancing);
        }

        [Fact]
        public void Rebalancing_DesvioMenorQue2_NaoSugereTrade()
        {
            var fake = new FakePerformanceCalculator
            {
                Result = MakePerformance(10000m, ("AAA", 51.5m, 5150m), ("BBB", 48.5m, 4850m))
            };
            var portfolio = MakePortfolio(("AAA", 0.50m), ("BBB", 0.50m));
            var assetsBySymbol = new Dictionary<string, Asset>
            {
                ["AAA"] = new Asset { Symbol = "AAA", CurrentPrice = 10m },
                ["BBB"] = new Asset { Symbol = "BBB", CurrentPrice = 10m }
            };
            var sut = CreateSut(fake);

            var result = sut.Optimize(portfolio, assetsBySymbol);

            Assert.False(result.NeedsRebalancing);
            Assert.Empty(result.SuggestedTrades);
        }

        [Fact]
        public void Rebalancing_TradeAbaixoDe100Reais_EhFiltrado()
        {
            var fake = new FakePerformanceCalculator
            {
                Result = MakePerformance(1000m, ("AAA", 10m, 100m), ("BBB", 90m, 900m))
            };
            var portfolio = MakePortfolio(("AAA", 0.05m), ("BBB", 0.95m));
            var assetsBySymbol = new Dictionary<string, Asset>
            {
                ["AAA"] = new Asset { Symbol = "AAA", CurrentPrice = 10m },
                ["BBB"] = new Asset { Symbol = "BBB", CurrentPrice = 10m }
            };
            var sut = CreateSut(fake);

            var result = sut.Optimize(portfolio, assetsBySymbol);

            Assert.DoesNotContain(result.SuggestedTrades, t => t.Symbol == "AAA");
        }

        [Fact]
        public void TargetAllocation_NaoSoma100_EhNormalizada()
        {
            var fake = new FakePerformanceCalculator
            {
                Result = MakePerformance(10000m, ("AAA", 60m, 6000m), ("BBB", 40m, 4000m))
            };
            // Soma dos targets = 60% (30%+30%) -> deveria normalizar para 50%/50%
            var portfolio = MakePortfolio(("AAA", 0.30m), ("BBB", 0.30m));
            var assetsBySymbol = new Dictionary<string, Asset>
            {
                ["AAA"] = new Asset { Symbol = "AAA", CurrentPrice = 10m },
                ["BBB"] = new Asset { Symbol = "BBB", CurrentPrice = 10m }
            };
            var sut = CreateSut(fake);

            var result = sut.Optimize(portfolio, assetsBySymbol);

            var aaaAllocation = result.CurrentAllocation.Single(a => a.Symbol == "AAA");
            Assert.Equal(50m, aaaAllocation.TargetWeight, precision: 2);
        }
    }
}
