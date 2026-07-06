using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Service.Interface
{
    public interface IMarketDataProvider
    {
        MarketData Current { get; }
    }
}
