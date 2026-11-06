using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KmValidator.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KmValidatorDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<KmValidatorDbContext>>();

        try
        {
            logger.LogInformation("Garantindo estrutura do banco PostgreSQL...");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Banco de dados pronto para ingestão de telemetria real de relógios.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao inicializar banco PostgreSQL.");
        }
    }
}
