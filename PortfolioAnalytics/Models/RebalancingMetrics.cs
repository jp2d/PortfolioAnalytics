namespace PortfolioAnalytics.Models
{
    public class RebalancingMetrics
    {
        public bool NeedsRebalancing { get; set; }
        public List<AllocationItem> CurrentAllocation { get; set; } = new();
        public List<SuggestedTrade> SuggestedTrades { get; set; } = new();
        public decimal TotalTransactionCost { get; set; }
        public string ExpectedImprovement { get; set; } = "Nenhum trade sugerido.";
    }
}
