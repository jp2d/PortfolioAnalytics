using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Service.Interface
{
    public interface IPerformanceCalculator
    {
        PerformanceMetrics Calculate(Portfolio portfolio, IReadOnlyDictionary<string, Asset> assetsBySymbol);
    }
}
