using PortfolioAnalytics.Models;
using System.Text.Json;

namespace PortfolioAnalytics.Data
{
    public static class DataSeeder
    {
        public static void Seed(DataContext context, string jsonPath, out MarketData marketData)
        {
            var json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 1) Assets
            var assets = JsonSerializer.Deserialize<List<Asset>>(
                root.GetProperty("assets").GetRawText(), options) ?? new();

            // 2) PriceHistory (dicionário separado -> join manual por symbol)
            var priceHistoryRaw = JsonSerializer.Deserialize<Dictionary<string, List<PricePoint>>>(
                root.GetProperty("priceHistory").GetRawText(), options) ?? new();

            foreach (var asset in assets)
            {
                asset.PriceHistory = priceHistoryRaw.TryGetValue(asset.Symbol, out var history)
                    ? history
                    : new List<PricePoint>();
            }

            // 3) Portfolios (sem "id" no JSON -> Id = UserId)
            var portfolios = JsonSerializer.Deserialize<List<Portfolio>>(
                root.GetProperty("portfolios").GetRawText(), options) ?? new();

            foreach (var portfolio in portfolios)
            {
                portfolio.Id = portfolio.UserId;
            }

            // 4) MarketData (singleton, não entra no DbContext)
            var marketDataElement = root.GetProperty("marketData");
            marketData = new MarketData
            {
                SelicRate = marketDataElement.GetProperty("selicRate").GetDecimal(),
                Ibov = marketDataElement.TryGetProperty("indexPerformance", out var idx) &&
                       idx.TryGetProperty("IBOV", out var ibov)
                    ? JsonSerializer.Deserialize<IndexPerformance>(ibov.GetRawText(), options)
                    : null
            };

            context.Assets.AddRange(assets);
            context.Portfolios.AddRange(portfolios);
            context.SaveChanges();
        }
    }
}
