using Microsoft.AspNetCore.Mvc;
using PortfolioAnalytics.Data;
using PortfolioAnalytics.Models.DTO;
using PortfolioAnalytics.Service.Interface;

namespace PortfolioAnalytics.Controllers
{
    [ApiController]
    [Route("api/portfolios")]
    public class AnalyticsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IPerformanceCalculator _performanceCalculator;

        public AnalyticsController(DataContext context, IPerformanceCalculator performanceCalculator)
        {
            _context = context;
            _performanceCalculator = performanceCalculator;
        }

        [HttpGet("{id}/performance")]
        public ActionResult<PerformanceResponseDto> GetPerformance(string id)
        {
            var portfolio = _context.Portfolios.FirstOrDefault(p => p.Id == id);
            if (portfolio is null)
                return NotFound(new { message = $"Portfólio '{id}' não encontrado." });

            var assetsBySymbol = _context.Assets.ToDictionary(a => a.Symbol);
            var metrics = _performanceCalculator.Calculate(portfolio, assetsBySymbol);

            var dto = new PerformanceResponseDto(
                metrics.TotalInvestment,
                metrics.CurrentValue,
                metrics.TotalReturnPercent,
                metrics.CurrentValue - metrics.TotalInvestment,
                metrics.AnnualizedReturnPercent,
                metrics.VolatilityPercent,
                metrics.Positions.Select(p => new PositionPerformanceDto(
                    p.Symbol, p.InvestedAmount, p.CurrentValue, p.ReturnPercent, p.WeightPercent
                )).ToList()
            );

            return Ok(dto);
        }
    }
}
