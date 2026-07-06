using PortfolioAnalytics.Models.Enum;

namespace PortfolioAnalytics.Models
{
    public class SectorExposure
    {
        public string Sector { get; set; } = default!;
        public decimal Percentage { get; set; }
        public RiskLevel Risk { get; set; }
    }
}
