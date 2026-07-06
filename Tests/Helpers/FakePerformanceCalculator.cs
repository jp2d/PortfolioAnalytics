using PortfolioAnalytics.Models;
using PortfolioAnalytics.Service;
using PortfolioAnalytics.Service.Interface;

namespace Tests.Helpers
{
    public class FakePerformanceCalculator : IPerformanceCalculator
    {
        public PerformanceMetrics Result { get; set; } = new();
        public PerformanceMetrics Calculate(Portfolio portfolio, IReadOnlyDictionary<string, Asset> assetsBySymbol) => Result;
    }
}
