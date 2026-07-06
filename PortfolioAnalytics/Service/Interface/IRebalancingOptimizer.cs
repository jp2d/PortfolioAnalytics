using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Service.Interface
{
    public interface IRebalancingOptimizer
    {
        RebalancingMetrics Optimize(Portfolio portfolio, IReadOnlyDictionary<string, Asset> assetsBySymbol);
    }
}
