namespace PortfolioAnalytics.Models.DTO
{
    public record PerformanceResponseDto(
    decimal TotalInvestment,
    decimal CurrentValue,
    decimal? TotalReturn,
    decimal TotalReturnAmount,
    decimal? AnnualizedReturn,
    decimal? Volatility,
    List<PositionPerformanceDto> PositionsPerformance
    );
}
