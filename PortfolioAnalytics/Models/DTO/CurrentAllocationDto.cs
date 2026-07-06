namespace PortfolioAnalytics.Models.DTO
{
    public record CurrentAllocationDto(
            string Symbol, 
            decimal CurrentWeight, 
            decimal TargetWeight, 
            decimal Deviation
        );
}
