namespace PortfolioAnalytics.Models
{
    public class Position
    {
        public string AssetSymbol { get; set; } = default!;
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal TargetAllocation { get; set; }   // fração: 0.20 = 20%
        public DateTime? LastTransaction { get; set; }
    }
}
