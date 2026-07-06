namespace PortfolioAnalytics.Models.DTO
{
    public record RebalancingResponseDto(
    bool NeedsRebalancing,
    List<CurrentAllocationDto> CurrentAllocation,
    List<SuggestedTradeDto> SuggestedTrades,
    decimal TotalTransactionCost,
    string ExpectedImprovement
);
}
