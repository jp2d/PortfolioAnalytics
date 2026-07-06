namespace PortfolioAnalytics.Models
{
    public class PositionConcentration
    {
        public string Symbol { get; set; } = default!;
        public decimal Percentage { get; set; }
    }
}
