namespace PortfolioAnalytics.Models.DTO
{
    public record ConcentrationRiskDto(
    LargestPositionDto LargestPosition,
    decimal Top3Concentration
    );
}
