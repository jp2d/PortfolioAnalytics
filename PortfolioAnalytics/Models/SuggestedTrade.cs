using PortfolioAnalytics.Models.Enum;

namespace PortfolioAnalytics.Models
{
    public class SuggestedTrade
    {
        public string Symbol { get; set; } = default!;
        public TradeAction Action { get; set; }
        public int Quantity { get; set; }
        public decimal EstimatedValue { get; set; }
        public decimal TransactionCost { get; set; }
        public string Reason { get; set; } = default!;
        public decimal AbsDeviation { get; set; } // usado só pra ordenação, não vai pro DTO
    }
}
