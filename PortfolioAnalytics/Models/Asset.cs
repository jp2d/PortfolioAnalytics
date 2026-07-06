namespace PortfolioAnalytics.Models
{
    public class Asset
    {
        public string Symbol { get; set; } = default!;   // chave primária
        public string Name { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Sector { get; set; } = default!;
        public decimal CurrentPrice { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<PricePoint> PriceHistory { get; set; } = new(); // preenchido no seeder via join
    }
}
