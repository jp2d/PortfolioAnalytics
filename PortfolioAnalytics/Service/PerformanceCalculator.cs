using PortfolioAnalytics.Models;
using PortfolioAnalytics.Service.Interface;

namespace PortfolioAnalytics.Service
{
    public class PerformanceCalculator : IPerformanceCalculator
    {
        private readonly IClock _clock;
        private readonly ILogger<PerformanceCalculator> _logger;

        public PerformanceCalculator(IClock clock, ILogger<PerformanceCalculator> logger)
        {
            _clock = clock;
            _logger = logger;
        }

        public PerformanceMetrics Calculate(Portfolio portfolio, IReadOnlyDictionary<string, Asset> assetsBySymbol)
        {
            var result = new PerformanceMetrics { TotalInvestment = portfolio.TotalInvestment };
            var positionMetrics = new List<PositionPerformanceMetric>();
            var aggregatedSeries = new Dictionary<DateOnly, decimal>();
            decimal currentValueTotal = 0m;
            int assetsWithHistory = 0;

            foreach (var position in portfolio.Positions)
            {
                if (!assetsBySymbol.TryGetValue(position.AssetSymbol, out var asset))
                {
                    _logger.LogWarning("Posição {Symbol} sem asset correspondente — excluída do cálculo.", position.AssetSymbol);
                    continue;
                }

                var currentPrice = ResolveCurrentPrice(asset, position.AveragePrice);
                var investedAmount = position.Quantity * position.AveragePrice;
                var currentValue = position.Quantity * currentPrice;
                currentValueTotal += currentValue;

                decimal? returnPercent = null;
                if (position.AveragePrice > 0)
                    returnPercent = (currentPrice - position.AveragePrice) / position.AveragePrice * 100m;
                else
                    _logger.LogWarning("AveragePrice <= 0 em {Symbol} — retorno da posição = null.", position.AssetSymbol);

                positionMetrics.Add(new PositionPerformanceMetric
                {
                    Symbol = position.AssetSymbol,
                    InvestedAmount = investedAmount,
                    CurrentValue = currentValue,
                    ReturnPercent = returnPercent
                });

                var investedSum = positionMetrics.Sum(p => p.InvestedAmount);
                if (Math.Abs(investedSum - portfolio.TotalInvestment) > 0.01m)
                {
                    _logger.LogWarning(
                        "Portfólio {Id}: TotalInvestment do seed ({Declared}) diverge da soma das posições ({Sum}). Usando o campo declarado.",
                        portfolio.Id, portfolio.TotalInvestment, investedSum);
                }

                if (asset.PriceHistory.Count > 0)
                {
                    assetsWithHistory++;
                    foreach (var point in asset.PriceHistory)
                    {
                        aggregatedSeries.TryGetValue(point.Date, out var existing);
                        aggregatedSeries[point.Date] = existing + position.Quantity * point.Price;
                    }
                }
            }

            result.CurrentValue = currentValueTotal;

            foreach (var pm in positionMetrics)
                pm.WeightPercent = currentValueTotal > 0 ? pm.CurrentValue / currentValueTotal * 100m : 0m;
            result.Positions = positionMetrics;

            if (portfolio.TotalInvestment > 0)
            {
                result.TotalReturnPercent = (currentValueTotal - portfolio.TotalInvestment) / portfolio.TotalInvestment * 100m;
            }
            else
            {
                _logger.LogWarning("TotalInvestment <= 0 no portfólio {Id} — totalReturn/annualizedReturn = null.", portfolio.Id);
            }

            if (result.TotalReturnPercent is decimal tr && portfolio.CreatedAt is DateTime createdAt)
            {
                var days = (_clock.Now.Date - createdAt.Date).Days;
                var annualizedFraction = FinancialMath.AnnualizeReturn((double)(tr / 100m), days);
                result.AnnualizedReturnPercent = annualizedFraction is double af ? (decimal)(af * 100.0) : null;
            }

            if (aggregatedSeries.Count >= 2)
            {
                _logger.LogInformation(
                    "Volatilidade calculada com cobertura parcial: {Covered}/{Total} posições com histórico.",
                    assetsWithHistory, portfolio.Positions.Count);

                var ordered = aggregatedSeries.OrderBy(kv => kv.Key).Select(kv => (double)kv.Value).ToList();
                var dailyReturns = new List<double>();
                for (int i = 1; i < ordered.Count; i++)
                {
                    if (ordered[i - 1] <= 0) continue;
                    dailyReturns.Add(ordered[i] / ordered[i - 1] - 1.0);
                }

                var stdDev = FinancialMath.SampleStdDev(dailyReturns);
                result.VolatilityPercent = stdDev is double sd
                    ? (decimal)(FinancialMath.AnnualizeVolatility(sd) * 100.0)
                    : null;
            }
            else
            {
                result.VolatilityPercent = null;
                _logger.LogInformation("Sem histórico suficiente no portfólio {Id} — volatility = null.", portfolio.Id);
            }

            return result;
        }

        private decimal ResolveCurrentPrice(Asset asset, decimal averagePriceFallback)
        {
            if (asset.CurrentPrice > 0) return asset.CurrentPrice;

            _logger.LogWarning("CurrentPrice inválido para {Symbol} — aplicando fallback.", asset.Symbol);

            var lastHistoryPrice = asset.PriceHistory
                .OrderByDescending(p => p.Date)
                .Select(p => p.Price)
                .FirstOrDefault(p => p > 0);

            return lastHistoryPrice > 0 ? lastHistoryPrice : averagePriceFallback;
        }
    }
}
