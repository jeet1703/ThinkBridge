using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;
using QuotesApi.Repositories;
using QuotesApi.Services;

namespace QuotesApi.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("QuotesDb")
            ?? "Data Source=quotes.db";

        services.AddDbContext<QuotesDbContext>(options =>
            options.UseSqlite(connectionString));

        // Transient lifetime: a new instance is created every time it's resolved.
        services.AddTransient<QuoteLogger>();

        // Scoped lifetime: one instance per HTTP request.
        services.AddScoped<IQuoteRepository, QuoteRepository>();

        // Singleton lifetime: a single instance shared across the entire application lifecycle.
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(
        this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuotesDbContext>();

        await db.Database.MigrateAsync();
    }
}