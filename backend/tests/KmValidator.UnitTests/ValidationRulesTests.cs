using FluentAssertions;
using KmValidator.Domain.Entities;
using KmValidator.Domain.Enums;
using KmValidator.Domain.Services.Validation;
using KmValidator.Domain.Services.Validation.Rules;
using KmValidator.Domain.ValueObjects;
using Xunit;

namespace KmValidator.UnitTests;

public class ValidationRulesTests
{
    [Fact]
    public void Coordinates_Haversine_ShouldCalculateDistanceAccurately()
    {
        // Ponto 1: Marco Zero SP (-23.550520, -46.633308)
        // Ponto 2: Parque Ibirapuera SP (-23.587416, -46.657634)
        var p1 = new Coordinates(-23.550520, -46.633308);
        var p2 = new Coordinates(-23.587416, -46.657634);

        var distance = p1.DistanceTo(p2);

        // Distância em linha reta é de ~4.8 a 4.9 km
        distance.Kilometers.Should().BeInRange(4.7, 5.0);
    }

    [Fact]
    public async Task PaceAndSpeedRule_ShouldRejectVehicleSpeedInRunning()
    {
        var rule = new PaceAndSpeedLimitRule();
        var athleteId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddMinutes(-10);
        var end = DateTime.UtcNow;

        // 10 km em 10 minutos = 60 km/h em "Corrida"
        var activity = new PhysicalActivity(
            athleteId,
            deviceId,
            ActivityType.Running,
            "Treino em Rodovia",
            start,
            end,
            reportedDistanceKm: 10.0,
            caloriesBurned: 100,
            avgHeartRateBpm: 70,
            maxHeartRateBpm: 75
        );

        var result = await rule.ValidateAsync(activity);

        result.Passed.Should().BeFalse();
        result.Severity.Should().Be(IssueSeverity.Critical);
        result.Message.Should().Contain("incompatível com corrida humana");
    }

    [Fact]
    public async Task BiometricCorrelationRule_ShouldDetectPassiveTransitWithLowHeartRate()
    {
        var rule = new BiometricEffortCorrelationRule();
        var start = DateTime.UtcNow.AddMinutes(-30);
        var end = DateTime.UtcNow;

        // 8 km em 30 min = 16 km/h (ritmo fortíssimo), mas com 58 bpm (repouso)
        var activity = new PhysicalActivity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ActivityType.Running,
            "Corrida Rápida Sem Esforço",
            start,
            end,
            reportedDistanceKm: 8.0,
            caloriesBurned: 200,
            avgHeartRateBpm: 58,
            maxHeartRateBpm: 62
        );

        var result = await rule.ValidateAsync(activity);

        result.Passed.Should().BeFalse();
        result.Severity.Should().Be(IssueSeverity.Critical);
        result.Message.Should().Contain("Anomalia biométrica detectada");
    }

    [Fact]
    public async Task ActivityKmValidationService_ShouldApproveValidRunningActivity()
    {
        var rules = new IActivityValidationRule[]
        {
            new GpsDistanceHaversineRule(),
            new PaceAndSpeedLimitRule(),
            new BiometricEffortCorrelationRule(),
            new GpsJitterTeleportationRule()
        };
        var engine = new ActivityKmValidationService(rules);

        var start = DateTime.UtcNow.AddMinutes(-30);
        var end = DateTime.UtcNow;

        // 5 km em 30 minutos = 10 km/h (pace 6:00 min/km), 150 bpm médio -> treino perfeito
        var activity = new PhysicalActivity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ActivityType.Running,
            "Corrida Parque 5K",
            start,
            end,
            reportedDistanceKm: 5.0,
            caloriesBurned: 350,
            avgHeartRateBpm: 152,
            maxHeartRateBpm: 168
        );

        // Adiciona rota coerente
        double baseLat = -23.5874;
        double baseLon = -46.6576;
        for (int i = 0; i <= 10; i++)
        {
            activity.AddRoutePoint(
                baseLat + (i * 0.004),
                baseLon + (i * 0.003),
                750.0,
                start.AddMinutes(i * 3),
                10.0,
                150 + i
            );
        }

        var validation = await engine.ValidateActivityAsync(activity);

        activity.Status.Should().Be(ValidationStatus.Valid);
        activity.ValidatedDistanceKm.Should().Be(5.0);
        validation.Status.Should().Be(ValidationStatus.Valid);
    }

    [Fact]
    public async Task ActivityKmValidationService_ShouldApproveValidStrengthTrainingActivity()
    {
        var rules = new IActivityValidationRule[]
        {
            new GpsDistanceHaversineRule(),
            new PaceAndSpeedLimitRule(),
            new BiometricEffortCorrelationRule(),
            new GpsJitterTeleportationRule()
        };
        var engine = new ActivityKmValidationService(rules);

        var start = DateTime.UtcNow.AddMinutes(-45);
        var end = DateTime.UtcNow;

        // Musculação com 0 KM, 45 min, 135 bpm médio, 400 kcal (estática indoor)
        var activity = new PhysicalActivity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ActivityType.StrengthTraining,
            "Treino de Hipertrofia Mi Band 10",
            start,
            end,
            reportedDistanceKm: 0.0,
            caloriesBurned: 400,
            avgHeartRateBpm: 135,
            maxHeartRateBpm: 168
        );

        var validation = await engine.ValidateActivityAsync(activity);

        activity.Status.Should().Be(ValidationStatus.Valid);
        activity.ValidatedDistanceKm.Should().Be(0.0);
        validation.Status.Should().Be(ValidationStatus.Valid);
        validation.SummaryNotes.Should().Contain("Musculação");
    }
}
