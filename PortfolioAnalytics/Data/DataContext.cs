using Microsoft.EntityFrameworkCore;
using PortfolioAnalytics.Models;

namespace PortfolioAnalytics.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Portfolio> Portfolios => Set<Portfolio>();
        public DbSet<Asset> Assets => Set<Asset>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Portfolio>(p =>
            {
                p.HasKey(x => x.Id);
                p.OwnsMany(x => x.Positions, pos =>
                {
                    pos.WithOwner();
                    pos.Property(x => x.AssetSymbol);
                });
            });

            modelBuilder.Entity<Asset>(a =>
            {
                a.HasKey(x => x.Symbol);
                a.OwnsMany(x => x.PriceHistory, ph =>
                {
                    ph.WithOwner();
                });
            });
        }
    }
}
