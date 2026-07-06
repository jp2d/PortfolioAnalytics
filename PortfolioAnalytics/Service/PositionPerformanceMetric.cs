namespace PortfolioAnalytics.Service
{
    public class PositionPerformanceMetric
    {
        public string Symbol { get; set; } = default!;
        public decimal InvestedAmount { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal? ReturnPercent { get; set; }
        public decimal WeightPercent { get; set; }
    }
}