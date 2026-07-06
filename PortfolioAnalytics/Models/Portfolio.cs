namespace PortfolioAnalytics.Models
{
    public class Portfolio
    {
        public string Id { get; set; } = default!;   // = userId (ver decisão acima)
        public string Name { get; set; } = default!;
        public string UserId { get; set; } = default!;
        public decimal TotalInvestment { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<Position> Positions { get; set; } = new();
    }
}
