namespace PortfolioAnalytics.Models.DTO
{
    public record RiskAnalysisResponseDto(
    string OverallRisk,
    decimal? SharpeRatio,
    ConcentrationRiskDto ConcentrationRisk,
    List<SectorDiversificationDto> SectorDiversification,
    List<string> Recommendations
    );
}
