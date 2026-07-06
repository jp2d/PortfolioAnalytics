namespace PortfolioAnalytics.Models.DTO
{
    public record PositionPerformanceDto(
    string Symbol,
    decimal InvestedAmount,
    decimal CurrentValue,
    decimal? Return,
    decimal Weight
    );
}
