using PortfolioAnalytics.Models;
using PortfolioAnalytics.Models.Enum;
using PortfolioAnalytics.Service.Interface;

namespace PortfolioAnalytics.Service
{
    public class RebalancingOptimizer : IRebalancingOptimizer
    {
        private readonly IPerformanceCalculator _performanceCalculator;
        private readonly ILogger<RebalancingOptimizer> _logger;

        public RebalancingOptimizer(IPerformanceCalculator performanceCalculator, ILogger<RebalancingOptimizer> logger)
        {
            _performanceCalculator = performanceCalculator;
            _logger = logger;
        }

        public RebalancingMetrics Optimize(Portfolio portfolio, IReadOnlyDictionary<string, Asset> assetsBySymbol)
        {
            var performance = _performanceCalculator.Calculate(portfolio, assetsBySymbol);
            var totalValue = performance.CurrentValue;
            var result = new RebalancingMetrics();

            if (totalValue <= 0 || performance.Positions.Count == 0)
            {
                _logger.LogWarning("Portfólio {Id}: valor total <= 0, rebalanceamento não aplicável.", portfolio.Id);
                return result;
            }

            // --- 1) Juntar peso atual (da performance) com target (do portfolio) ---
            var targetBySymbol = portfolio.Positions.ToDictionary(p => p.AssetSymbol, p => p.TargetAllocation);

            var targetSum = performance.Positions.Sum(p => targetBySymbol.GetValueOrDefault(p.Symbol, 0m));
            var normalize = targetSum > 0 && Math.Abs(targetSum - 1m) > 0.0001m;

            if (normalize)
            {
                _logger.LogWarning(
                    "Portfólio {Id}: soma de TargetAllocation = {Sum:P1} (≠ 100%). Normalizando proporcionalmente.",
                    portfolio.Id, targetSum);
            }

            // --- 2) Montar alocação atual x alvo ---
            var allocationItems = new List<(string Symbol, decimal CurrentWeight, decimal TargetWeight, decimal CurrentValue)>();

            foreach (var pos in performance.Positions)
            {
                var rawTarget = targetBySymbol.GetValueOrDefault(pos.Symbol, 0m);
                var normalizedTarget = normalize && targetSum > 0 ? rawTarget / targetSum : rawTarget;
                var targetWeight = normalizedTarget * 100m;

                allocationItems.Add((pos.Symbol, pos.WeightPercent, targetWeight, pos.CurrentValue));
            }

            result.CurrentAllocation = allocationItems
                .Select(a => new AllocationItem
                {
                    Symbol = a.Symbol,
                    CurrentWeight = a.CurrentWeight,
                    TargetWeight = a.TargetWeight,
                    Deviation = a.CurrentWeight - a.TargetWeight
                })
                .ToList();

            // --- 3) Gerar candidatos a trade (desvio > 2%) ---
            var trades = new List<SuggestedTrade>();

            foreach (var item in allocationItems)
            {
                var deviation = item.CurrentWeight - item.TargetWeight;
                if (Math.Abs(deviation) <= AnalyticsRules.RebalanceDeviationThreshold) continue;

                if (!assetsBySymbol.TryGetValue(item.Symbol, out var asset) || asset.CurrentPrice <= 0)
                {
                    _logger.LogWarning("Symbol {Symbol}: sem preço válido para calcular trade — ignorado.", item.Symbol);
                    continue;
                }

                // Delta financeiro necessário para atingir o alvo (negativo = precisa vender)
                var delta = (item.TargetWeight - item.CurrentWeight) / 100m * totalValue;
                var rawQuantity = Math.Abs(delta) / asset.CurrentPrice;

                var bestQuantity = ChooseOptimalQuantity(rawQuantity, item, asset.CurrentPrice, totalValue);
                if (bestQuantity is null)
                {
                    _logger.LogInformation(
                        "Symbol {Symbol}: nem floor nem ceil da quantidade atingem o valor mínimo de {Min:C} — descartado.",
                        item.Symbol, AnalyticsRules.MinTradeValue);
                    continue;
                }

                var quantity = bestQuantity.Value;
                var estimatedValue = quantity * asset.CurrentPrice;

                var action = delta < 0 ? TradeAction.Sell : TradeAction.Buy;
                var reason = action == TradeAction.Sell
                    ? $"Reduzir de {item.CurrentWeight:F1}% para {item.TargetWeight:F1}%"
                    : $"Aumentar de {item.CurrentWeight:F1}% para {item.TargetWeight:F1}%";

                trades.Add(new SuggestedTrade
                {
                    Symbol = item.Symbol,
                    Action = action,
                    Quantity = quantity,
                    EstimatedValue = estimatedValue,
                    TransactionCost = estimatedValue * AnalyticsRules.TransactionCostRate,
                    Reason = reason,
                    AbsDeviation = Math.Abs(deviation)
                });
            }

            // --- 4) Priorizar maiores desvios ---
            trades = trades.OrderByDescending(t => t.AbsDeviation).ToList();

            result.SuggestedTrades = trades;
            result.NeedsRebalancing = trades.Count > 0;
            result.TotalTransactionCost = trades.Sum(t => t.TransactionCost);

            // --- 5) Expected improvement: simular concentração top-3 antes/depois ---
            result.ExpectedImprovement = trades.Count > 0
                ? BuildExpectedImprovement(allocationItems, trades)
                : "Portfólio já alinhado ao alvo (nenhum desvio acima de 2%).";

            return result;
        }

        /// <summary>
        /// Escolhe entre floor(rawQuantity) e ceil(rawQuantity) a opção que resulta no MENOR desvio
        /// residual após o trade, respeitando o valor mínimo de R$100 e sem deixar o desvio "passar"
        /// para o lado oposto do alvo além da tolerância de rebalanceamento (2%).
        /// Isso corrige a sub-correção sistemática do floor puro e resgata trades que ficariam
        /// abaixo de R$100 só por causa do arredondamento para baixo.
        /// </summary>
        private static int? ChooseOptimalQuantity(
            decimal rawQuantity,
            (string Symbol, decimal CurrentWeight, decimal TargetWeight, decimal CurrentValue) item,
            decimal price,
            decimal totalValue)
        {
            var candidates = new[] { (int)Math.Floor(rawQuantity), (int)Math.Ceiling(rawQuantity) }
                .Distinct()
                .Where(q => q > 0);

            var isSell = item.CurrentWeight > item.TargetWeight;
            var originalSign = Math.Sign(item.CurrentWeight - item.TargetWeight);

            int? bestQuantity = null;
            var bestResidualDeviation = decimal.MaxValue;

            foreach (var candidateQty in candidates)
            {
                var candidateValue = candidateQty * price;
                if (candidateValue < AnalyticsRules.MinTradeValue) continue;

                var signedValue = isSell ? -candidateValue : candidateValue;
                var newValue = item.CurrentValue + signedValue;
                var newWeight = totalValue > 0 ? newValue / totalValue * 100m : 0m;
                var residualDeviation = Math.Abs(newWeight - item.TargetWeight);
                var newSign = Math.Sign(newWeight - item.TargetWeight);

                // Rejeita se "passou do alvo" para o lado oposto além da tolerância de rebalanceamento
                var overshootsBeyondTolerance =
                    newSign != 0 && newSign != originalSign && residualDeviation > AnalyticsRules.RebalanceDeviationThreshold;
                if (overshootsBeyondTolerance) continue;

                if (residualDeviation < bestResidualDeviation)
                {
                    bestResidualDeviation = residualDeviation;
                    bestQuantity = candidateQty;
                }
            }

            return bestQuantity;
        }

        private string BuildExpectedImprovement(
            List<(string Symbol, decimal CurrentWeight, decimal TargetWeight, decimal CurrentValue)> allocationItems,
            List<SuggestedTrade> trades)
        {
            var beforeTop3 = allocationItems
                .OrderByDescending(a => a.CurrentWeight)
                .Take(3)
                .Sum(a => a.CurrentWeight);

            // Simula os novos valores após os trades (posições sem trade ficam inalteradas)
            var simulatedValues = allocationItems.ToDictionary(a => a.Symbol, a => a.CurrentValue);

            foreach (var trade in trades)
            {
                var signedDelta = trade.Action == TradeAction.Sell ? -trade.EstimatedValue : trade.EstimatedValue;
                simulatedValues[trade.Symbol] += signedDelta;
            }

            var newTotal = simulatedValues.Values.Sum();
            if (newTotal <= 0) return "Não foi possível estimar a melhora esperada.";

            var afterTop3 = simulatedValues
                .Select(kv => kv.Value / newTotal * 100m)
                .OrderByDescending(w => w)
                .Take(3)
                .Sum();

            if (beforeTop3 <= 0) return "Não foi possível estimar a melhora esperada.";

            var reductionPercent = (beforeTop3 - afterTop3) / beforeTop3 * 100m;

            return reductionPercent > 0
                ? $"Redução estimada de {reductionPercent:F1}% na concentração das 3 maiores posições ({beforeTop3:F1}% → {afterTop3:F1}%)"
                : "Trades sugeridos não reduzem a concentração das 3 maiores posições nesta simulação.";
        }
    }
}
