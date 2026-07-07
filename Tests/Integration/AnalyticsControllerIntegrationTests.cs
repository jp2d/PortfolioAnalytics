using PortfolioAnalytics.Models.DTO;
using System.Net;
using System.Net.Http.Json;

namespace Tests.Integration
{
    public class AnalyticsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AnalyticsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetPerformance_PortfolioExistente_Retorna200ComDadosCorretos()
        {
            var response = await _client.GetAsync("/api/portfolios/user-001/performance");
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<PerformanceResponseDto>();

            Assert.NotNull(dto);
            Assert.Equal(100000m, dto!.TotalInvestment);
            Assert.Equal(5, dto.PositionsPerformance.Count);
        }

        [Fact]
        public async Task GetPerformance_PortfolioInexistente_Retorna404()
        {
            var response = await _client.GetAsync("/api/portfolios/nao-existe/performance");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetRiskAnalysis_PortfolioExistente_Retorna200ComRiscoAlto()
        {
            var response = await _client.GetAsync("/api/portfolios/user-003/risk-analysis");
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<RiskAnalysisResponseDto>();

            Assert.NotNull(dto);
            Assert.Equal("High", dto!.OverallRisk);
            Assert.NotEmpty(dto.Recommendations);
        }

        [Fact]
        public async Task GetRiskAnalysis_PortfolioInexistente_Retorna404()
        {
            var response = await _client.GetAsync("/api/portfolios/nao-existe/risk-analysis");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetRebalancing_PortfolioAlinhado_RetornaSemTrades()
        {
            // user-001: já validamos manualmente que nenhum desvio passa de 2%
            var response = await _client.GetAsync("/api/portfolios/user-001/rebalancing");
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<RebalancingResponseDto>();

            Assert.NotNull(dto);
            Assert.False(dto!.NeedsRebalancing);
            Assert.Empty(dto.SuggestedTrades);
        }

        [Fact]
        public async Task GetRebalancing_PortfolioDesbalanceado_RetornaTrades()
        {
            var response = await _client.GetAsync("/api/portfolios/user-003/rebalancing");
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<RebalancingResponseDto>();

            Assert.NotNull(dto);
            Assert.True(dto!.NeedsRebalancing);
            Assert.NotEmpty(dto.SuggestedTrades);
        }
    }
}
