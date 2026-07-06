using PortfolioAnalytics.Models.Enum;

namespace PortfolioAnalytics.Models
{
    public class RiskMetrics
    {
        public RiskLevel OverallRisk { get; set; }
        public decimal? SharpeRatio { get; set; }
        public PositionConcentration LargestPosition { get; set; } = default!;
        public decimal Top3ConcentrationPercent { get; set; }
        public List<SectorExposure> SectorDiversification { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}
