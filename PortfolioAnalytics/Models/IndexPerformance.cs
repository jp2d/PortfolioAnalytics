namespace PortfolioAnalytics.Models
{
    public class IndexPerformance
    {
        public decimal CurrentValue { get; set; }
        public decimal DailyChange { get; set; }
        public decimal MonthlyChange { get; set; }
        public decimal YearToDate { get; set; }
    }
}
