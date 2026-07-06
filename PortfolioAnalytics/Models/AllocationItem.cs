namespace PortfolioAnalytics.Models
{
    public class AllocationItem
    {
        public string Symbol { get; set; } = default!;
        public decimal CurrentWeight { get; set; }
        public decimal TargetWeight { get; set; }
        public decimal Deviation { get; set; } // currentWeight - targetWeight
    }
}
