using Microsoft.EntityFrameworkCore;
using PortfolioAnalytics.Data;
using PortfolioAnalytics.Service;
using PortfolioAnalytics.Service.Interface;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DataContext>(options =>
    options.UseInMemoryDatabase("PortfolioAnalyticsDb"));


builder.Services.AddSingleton<IClock, SeedReferenceClock>();
builder.Services.AddSingleton<IMarketDataProvider, MarketDataProvider>();

var app = builder.Build();

//var options = new JsonSerializerOptions
//{
//    PropertyNameCaseInsensitive = true
//};

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    var seedPath = Path.Combine(AppContext.BaseDirectory, "SeedData.json");
    DataSeeder.Seed(context, seedPath, out var marketData);

    MarketDataProvider.Instance = marketData;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
