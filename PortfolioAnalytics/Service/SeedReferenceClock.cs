using PortfolioAnalytics.Service.Interface;

namespace PortfolioAnalytics.Service
{
    public class SeedReferenceClock : IClock
    {
        public DateTime Now => new DateTime(2024, 10, 6, 10, 30, 0, DateTimeKind.Utc);
    }
}
