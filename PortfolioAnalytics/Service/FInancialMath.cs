using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Service
{
    public static class FinancialMath
    {
        public const int TradingDaysPerYear = 252;

        /// <summary>
        /// Retorno diário aritmético: (P_t / P_t-1) - 1.
        /// Pula pares com preço anterior <= 0 (guarda contra divisão por zero).
        /// </summary>
        public static List<double> DailyReturns(IReadOnlyList<PricePoint> priceHistory)
        {
            var ordered = priceHistory.OrderBy(p => p.Date).ToList();
            var returns = new List<double>();

            for (int i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1].Price;
                var curr = ordered[i].Price;
                if (prev <= 0) continue;
                returns.Add((double)(curr / prev) - 1.0);
            }

            return returns;
        }

        /// <summary>Desvio padrão amostral (divisor n-1). Exige ao menos 2 valores.</summary>
        public static double? SampleStdDev(IReadOnlyList<double> values)
        {
            if (values.Count < 2) return null;
            var mean = values.Average();
            var sumSq = values.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sumSq / (values.Count - 1));
        }

        /// <summary>Anualiza volatilidade diária: sigma_diaria * sqrt(252).</summary>
        public static double AnnualizeVolatility(double dailyStdDev) =>
            dailyStdDev * Math.Sqrt(TradingDaysPerYear);

        /// <summary>
        /// Anualiza retorno total: ((1+r)^(365/dias) - 1).
        /// ATENÇÃO: r é FRAÇÃO (0.085), não percentual (8.5) — armadilha clássica do teste.
        /// </summary>
        public static double? AnnualizeReturn(double totalReturnFraction, int days)
        {
            if (days <= 0) return null;
            var basis = 1 + totalReturnFraction;
            if (basis <= 0) return null; // evita raiz de número negativo
            return Math.Pow(basis, 365.0 / days) - 1.0;
        }
    }
}
