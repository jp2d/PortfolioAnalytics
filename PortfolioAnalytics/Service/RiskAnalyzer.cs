using PortfolioAnalytics.Models;
using PortfolioAnalytics.Models.Enum;
using PortfolioAnalytics.Service.Interface;

namespace PortfolioAnalytics.Service
{
    public class RiskAnalyzer : IRiskAnalyzer
    {
        private readonly IPerformanceCalculator _performanceCalculator;
        private readonly ILogger<RiskAnalyzer> _logger;

        public RiskAnalyzer(IPerformanceCalculator performanceCalculator, ILogger<RiskAnalyzer> logger)
        {
            _performanceCalculator = performanceCalculator;
            _logger = logger;
        }

        public RiskMetrics Analyze(Portfolio portfolio, IReadOnlyDictionary<string, Asset> assetsBySymbol, MarketData marketData)
        {
            // Reaproveita o Performance Calculator — nada de recalcular volatilidade/retorno aqui.
            var performance = _performanceCalculator.Calculate(portfolio, assetsBySymbol);

            var result = new RiskMetrics();

            // --- Sharpe Ratio ---
            if (performance.AnnualizedReturnPercent is decimal annualized &&
                performance.VolatilityPercent is decimal vol && vol > 0)
            {
                var selicPercent = marketData.SelicRate * 100m; // fração -> percentual
                result.SharpeRatio = (annualized - selicPercent) / vol;
            }
            else
            {
                _logger.LogInformation(
                    "Sharpe Ratio = null para portfólio {Id} (retorno anualizado ou volatilidade indisponível/zero).",
                    portfolio.Id);
            }

            // --- Concentração ---
            var orderedByWeight = performance.Positions.OrderByDescending(p => p.WeightPercent).ToList();

            result.LargestPosition = orderedByWeight.Count > 0
                ? new PositionConcentration
                {
                    Symbol = orderedByWeight[0].Symbol,
                    Percentage = orderedByWeight[0].WeightPercent
                }
                : new PositionConcentration { Symbol = "-", Percentage = 0m };

            result.Top3ConcentrationPercent = orderedByWeight.Take(3).Sum(p => p.WeightPercent);

            // --- Diversificação setorial ---
            var sectorGroups = performance.Positions
                .GroupBy(p => assetsBySymbol.TryGetValue(p.Symbol, out var asset) ? asset.Sector : "Unknown")
                .Select(g => new SectorExposure
                {
                    Sector = g.Key,
                    Percentage = g.Sum(p => p.WeightPercent),
                    Risk = ClassifyBySector(g.Sum(p => p.WeightPercent))
                })
                .OrderByDescending(s => s.Percentage)
                .ToList();

            result.SectorDiversification = sectorGroups;

            // --- Classificação geral e recomendações ---
            var recommendations = new List<string>();
            var worst = RiskLevel.Low;

            foreach (var pos in orderedByWeight)
            {
                var posRisk = ClassifyByPosition(pos.WeightPercent);
                if (posRisk > worst) worst = posRisk;

                if (posRisk == RiskLevel.High)
                    recommendations.Add($"Posição {pos.Symbol} representa {pos.WeightPercent:F1}% do portfólio (ideal < {AnalyticsRules.HighPositionThreshold:F0}%)");
            }

            foreach (var sector in sectorGroups)
            {
                if (sector.Risk > worst) worst = sector.Risk;

                if (sector.Risk == RiskLevel.High)
                    recommendations.Add($"Reduzir exposição ao setor {sector.Sector} ({sector.Percentage:F1}%)");
            }

            result.OverallRisk = worst;
            result.Recommendations = recommendations;

            return result;
        }

        private static RiskLevel ClassifyByPosition(decimal weightPercent) =>
            weightPercent > AnalyticsRules.HighPositionThreshold ? RiskLevel.High
            : weightPercent >= AnalyticsRules.MediumPositionThreshold ? RiskLevel.Medium
            : RiskLevel.Low;

        private static RiskLevel ClassifyBySector(decimal weightPercent) =>
            weightPercent > AnalyticsRules.HighSectorThreshold ? RiskLevel.High
            : weightPercent >= AnalyticsRules.MediumSectorThreshold ? RiskLevel.Medium
            : RiskLevel.Low;
    }
}
