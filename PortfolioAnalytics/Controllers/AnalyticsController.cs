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
        private readonly IRiskAnalyzer _riskAnalyzer;
        private readonly IMarketDataProvider _marketDataProvider;
        private readonly IRebalancingOptimizer _rebalancingOptimizer;

        public AnalyticsController(DataContext context, IPerformanceCalculator performanceCalculator,
                                   IRiskAnalyzer riskAnalyzer, IMarketDataProvider marketDataProvider,
                                   IRebalancingOptimizer rebalancingOptimizer)
        {
            _context = context;
            _performanceCalculator = performanceCalculator;
            _riskAnalyzer = riskAnalyzer;
            _marketDataProvider = marketDataProvider;
            _rebalancingOptimizer = rebalancingOptimizer;
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

        [HttpGet("{id}/risk-analysis")]
        public ActionResult<RiskAnalysisResponseDto> GetRiskAnalysis(string id)
        {
            var portfolio = _context.Portfolios.FirstOrDefault(p => p.Id == id);
            if (portfolio is null)
                return NotFound(new { message = $"Portfólio '{id}' não encontrado." });

            var assetsBySymbol = _context.Assets.ToDictionary(a => a.Symbol);
            var risk = _riskAnalyzer.Analyze(portfolio, assetsBySymbol, _marketDataProvider.Current);

            var dto = new RiskAnalysisResponseDto(
                risk.OverallRisk.ToString(),
                risk.SharpeRatio,
                new ConcentrationRiskDto(
                    new LargestPositionDto(risk.LargestPosition.Symbol, risk.LargestPosition.Percentage),
                    risk.Top3ConcentrationPercent
                ),
                risk.SectorDiversification.Select(s =>
                    new SectorDiversificationDto(s.Sector, s.Percentage, s.Risk.ToString())
                ).ToList(),
                risk.Recommendations
            );

            return Ok(dto);
        }

        [HttpGet("{id}/rebalancing")]
        public ActionResult<RebalancingResponseDto> GetRebalancing(string id)
        {
            var portfolio = _context.Portfolios.FirstOrDefault(p => p.Id == id);
            if (portfolio is null)
                return NotFound(new { message = $"Portfólio '{id}' não encontrado." });

            var assetsBySymbol = _context.Assets.ToDictionary(a => a.Symbol);
            var rebalancing = _rebalancingOptimizer.Optimize(portfolio, assetsBySymbol);

            var dto = new RebalancingResponseDto(
                rebalancing.NeedsRebalancing,
                rebalancing.CurrentAllocation.Select(a =>
                    new CurrentAllocationDto(a.Symbol, a.CurrentWeight, a.TargetWeight, a.Deviation)
                ).ToList(),
                rebalancing.SuggestedTrades.Select(t =>
                    new SuggestedTradeDto(t.Symbol, t.Action.ToString().ToUpperInvariant(), t.Quantity, t.EstimatedValue, t.TransactionCost, t.Reason)
                ).ToList(),
                rebalancing.TotalTransactionCost,
                rebalancing.ExpectedImprovement
            );

            return Ok(dto);
        }
    }
}
