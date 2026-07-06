namespace PortfolioAnalytics.Service
{
    public static class AnalyticsRules
    {
        // Concentração por posição individual
        public const decimal HighPositionThreshold = 25m;   // > 25% => Alto
        public const decimal MediumPositionThreshold = 15m; // 15%-25% => Médio (25% exato = Médio)

        // Concentração setorial
        public const decimal HighSectorThreshold = 40m;     // > 40% => Alto
        public const decimal MediumSectorThreshold = 25m;   // 25%-40% => Médio (40% exato = Médio)

        // Rebalanceamento 
        public const decimal RebalanceDeviationThreshold = 2m;
        public const decimal MinTradeValue = 100m;
        public const decimal TransactionCostRate = 0.003m;
    }
}
