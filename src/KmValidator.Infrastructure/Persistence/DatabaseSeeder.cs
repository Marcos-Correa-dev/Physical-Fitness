using KmValidator.Domain.Entities;
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

            // Garante apenas a existência de um perfil de atleta padrão caso o banco esteja vazio
            if (!await context.Athletes.AnyAsync())
            {
                var athlete = new Athlete("Atleta Physical Fitness", "atleta@physicalfitness.com", 72.0, 190, 60);
                await context.Athletes.AddAsync(athlete);
                await context.SaveChangesAsync();
                logger.LogInformation("Perfil de atleta inicial configurado.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao inicializar banco PostgreSQL.");
        }
    }
}
