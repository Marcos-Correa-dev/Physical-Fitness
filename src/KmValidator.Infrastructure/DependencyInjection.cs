using KmValidator.Application.Ports;
using KmValidator.Application.UseCases;
using KmValidator.Domain.Ports;
using KmValidator.Domain.Services.Validation;
using KmValidator.Domain.Services.Validation.Rules;
using KmValidator.Infrastructure.Persistence;
using KmValidator.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KmValidator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configuração do PostgreSQL
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=km_validator_db;Username=postgres;Password=admin;";

        services.AddDbContext<KmValidatorDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. Registro das Portas Secundárias (Repositories & UoW)
        services.AddScoped<IPhysicalActivityRepository, PhysicalActivityRepository>();
        services.AddScoped<IAthleteRepository, AthleteRepository>();
        services.AddScoped<IWearableDeviceRepository, WearableDeviceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 3. Regras de Validação S.O.L.I.D (Injetadas como IEnumerable<IActivityValidationRule>)
        services.AddTransient<IActivityValidationRule, GpsDistanceHaversineRule>();
        services.AddTransient<IActivityValidationRule, PaceAndSpeedLimitRule>();
        services.AddTransient<IActivityValidationRule, BiometricEffortCorrelationRule>();
        services.AddTransient<IActivityValidationRule, GpsJitterTeleportationRule>();

        // 4. Domain Service
        services.AddTransient<ActivityKmValidationService>();

        // 5. Portas Primárias / Casos de Uso da Aplicação
        services.AddScoped<IIngestActivityUseCase, IngestActivityUseCase>();
        services.AddScoped<IValidateActivityUseCase, ValidateActivityUseCase>();
        services.AddScoped<IGetDashboardMetricsUseCase, GetDashboardMetricsUseCase>();
        services.AddScoped<ISimulateSmartwatchSyncUseCase, SimulateSmartwatchSyncUseCase>();

        return services;
    }
}
