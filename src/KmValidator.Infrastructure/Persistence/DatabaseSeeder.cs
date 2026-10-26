using KmValidator.Application.DTOs;
using KmValidator.Application.Ports;
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
        var simulateUseCase = scope.ServiceProvider.GetRequiredService<ISimulateSmartwatchSyncUseCase>();

        try
        {
            logger.LogInformation("Garantindo estrutura do banco PostgreSQL...");
            await context.Database.EnsureCreatedAsync();

            if (!await context.PhysicalActivities.AnyAsync())
            {
                logger.LogInformation("Populando banco de dados com atividades iniciais do relógio...");

                // 1. Treino de Corrida 5K Válido no Ibirapuera
                await simulateUseCase.ExecuteAsync(new SimulationRequestDto
                {
                    Scenario = "valid_5k_run",
                    DeviceModel = "Garmin Forerunner 965"
                });

                // 2. Treino de Ciclismo Válido na Marginal
                await simulateUseCase.ExecuteAsync(new SimulationRequestDto
                {
                    Scenario = "valid_cycling",
                    DeviceModel = "Polar Vantage V3"
                });

                // 3. Tentativa de Fraude (Deslocamento de Carro com BPM baixo)
                await simulateUseCase.ExecuteAsync(new SimulationRequestDto
                {
                    Scenario = "fraud_vehicle_run",
                    DeviceModel = "Apple Watch Ultra 2"
                });

                // 4. Corrida com Erro de Salto GPS
                await simulateUseCase.ExecuteAsync(new SimulationRequestDto
                {
                    Scenario = "gps_glitch",
                    DeviceModel = "Samsung Galaxy Watch 6"
                });

                // 5. Treino de Musculação sugerido pela Xiaomi Mi Band 10
                await simulateUseCase.ExecuteAsync(new SimulationRequestDto
                {
                    Scenario = "miband_strength_training",
                    DeviceModel = "Xiaomi Mi Band 10"
                });

                logger.LogInformation("Dados iniciais de telemetria semeados com sucesso.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao inicializar e semear banco PostgreSQL.");
        }
    }
}
