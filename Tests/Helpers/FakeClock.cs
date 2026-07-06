using PortfolioAnalytics.Service.Interface;

namespace Tests.Helpers
{
    public class FakeClock : IClock
    {
        public FakeClock(DateTime now) => Now = now;
        public DateTime Now { get; }
    }
}
