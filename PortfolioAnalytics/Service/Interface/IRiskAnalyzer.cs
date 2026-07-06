using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Service.Interface
{
    public interface IRiskAnalyzer
    {
        RiskMetrics Analyze(Portfolio portfolio, IReadOnlyDictionary<string, Asset> assetsBySymbol, MarketData marketData);
    }
}
