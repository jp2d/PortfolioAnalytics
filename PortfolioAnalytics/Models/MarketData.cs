namespace PortfolioAnalytics.Models
{
    public class MarketData
    {
        public decimal SelicRate { get; set; }          // fração: 0.12 = 12% a.a.
        public IndexPerformance? Ibov { get; set; }
    }
}
