namespace PortfolioAnalytics.Service
{
    public class PerformanceMetrics
    {
        public decimal TotalInvestment { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal? TotalReturnPercent { get; set; }
        public decimal? AnnualizedReturnPercent { get; set; }
        public decimal? VolatilityPercent { get; set; }
        public List<PositionPerformanceMetric> Positions { get; set; } = new();
    }
}
