namespace PortfolioAnalytics.Models.DTO
{
    public record SuggestedTradeDto(
    string Symbol,
    string Action,
    int Quantity,
    decimal EstimatedValue,
    decimal TransactionCost,
    string Reason
);
}
