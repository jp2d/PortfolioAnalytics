using PortfolioAnalytics.Models;
using PortfolioAnalytics.Service.Interface;

namespace PortfolioAnalytics.Service
{
    public class MarketDataProvider : IMarketDataProvider
    {
        public static MarketData Instance { get; set; } = new();
        public MarketData Current => Instance;
    }
}
