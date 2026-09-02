using System;
using System.Linq;
using System.Threading.Tasks;
using CRManager.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CRManager.Api.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Checking for pending EF Core database migrations...");
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migration(s) to SQL Server...", pendingMigrations.Count());
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("EF Core migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("Database is up to date. No pending migrations.");
            }

            // Ensure any existing credit cards with 0 settlement day are updated to default 25
            await dbContext.Database.ExecuteSqlRawAsync("UPDATE CreditCards SET SettlementDay = 25 WHERE SettlementDay <= 0 OR SettlementDay > 31");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying EF Core database migrations: {Message}", ex.Message);
        }
    }
}
