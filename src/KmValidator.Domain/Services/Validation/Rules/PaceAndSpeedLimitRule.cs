using KmValidator.Domain.Entities;
using KmValidator.Domain.Enums;
using KmValidator.Domain.ValueObjects;

namespace KmValidator.Domain.Services.Validation.Rules;

public class PaceAndSpeedLimitRule : IActivityValidationRule
{
    public string RuleName => "HumanPaceAndSpeedLimit";
    public int ExecutionOrder => 2;

    public Task<ValidationRuleResult> ValidateAsync(PhysicalActivity activity, CancellationToken cancellationToken = default)
    {
        var duration = activity.GetDuration();

        // Tratamento para Musculação / Treino de Força (Indoor)
        if (activity.ActivityType == ActivityType.StrengthTraining)
        {
            if (duration.TotalMinutes < 10)
            {
                return Task.FromResult(ValidationRuleResult.Warning(
                    RuleName,
                    "Treino de musculação com duração inferior a 10 minutos.",
                    "Sessão muito curta para estímulo muscular significativo."));
            }

            if (activity.ReportedDistanceKm > 0.5)
            {
                return Task.FromResult(ValidationRuleResult.Warning(
                    RuleName,
                    $"Quilometragem detectada ({activity.ReportedDistanceKm:F2} km) em treino de musculação.",
                    "Musculação é uma atividade predominantemente estática em academia."));
            }

            return Task.FromResult(ValidationRuleResult.Success(
                RuleName,
                $"Treino de força indoor validado ({duration.TotalMinutes:F0} min de sessão contínua sem deslocamento espúrio).",
                0.0));
        }

        if (duration.TotalMinutes <= 0 || activity.ReportedDistanceKm <= 0)
        {
            return Task.FromResult(ValidationRuleResult.Failure(
                RuleName,
                "Duração ou distância zeradas/inválidas para validação de ritmo."));
        }

        double avgSpeedKmh = activity.ReportedDistanceKm / duration.TotalHours;
        var pace = activity.GetPace();

        // Verificar pontos individuais se existirem
        double maxPointSpeedKmh = activity.RoutePoints.Any()
            ? activity.RoutePoints.Max(p => p.InstantSpeedKmh)
            : avgSpeedKmh;

        switch (activity.ActivityType)
        {
            case ActivityType.Running:
                // Velocidade média acima de 30 km/h ou pontual acima de 45 km/h indica veículo
                if (avgSpeedKmh > 30.0 || maxPointSpeedKmh > 45.0)
                {
                    return Task.FromResult(ValidationRuleResult.Failure(
                        RuleName,
                        $"Velocidade incompatível com corrida humana. Média: {avgSpeedKmh:F1} km/h, Máx: {maxPointSpeedKmh:F1} km/h (Pace: {pace}).",
                        "Indício forte de deslocamento em veículo motorizado ou transporte público.",
                        avgSpeedKmh));
                }

                if (avgSpeedKmh > 23.0) // ~2:36 min/km (ritmo próximo do recorde mundial de maratona)
                {
                    return Task.FromResult(ValidationRuleResult.Warning(
                        RuleName,
                        $"Ritmo extremamente elevado para corrida: {pace} ({avgSpeedKmh:F1} km/h).",
                        "Supera padrões recreativos e exige revisão de atleta profissional.",
                        avgSpeedKmh));
                }
                break;

            case ActivityType.Walking:
                if (avgSpeedKmh > 12.0 || maxPointSpeedKmh > 15.0)
                {
                    return Task.FromResult(ValidationRuleResult.Failure(
                        RuleName,
                        $"Velocidade impossível para caminhada: {avgSpeedKmh:F1} km/h (Pace: {pace}).",
                        "Atividade classificada como caminhada possui velocidades de corrida ou veículo.",
                        avgSpeedKmh));
                }
                break;

            case ActivityType.Cycling:
                if (avgSpeedKmh > 65.0 || maxPointSpeedKmh > 100.0)
                {
                    return Task.FromResult(ValidationRuleResult.Failure(
                        RuleName,
                        $"Velocidade ciclística anormalmente alta: Média {avgSpeedKmh:F1} km/h, Máx {maxPointSpeedKmh:F1} km/h.",
                        "Velocidade excede capacidade humana em bicicleta convencional sem auxílio motorizado.",
                        avgSpeedKmh));
                }
                break;
        }

        return Task.FromResult(ValidationRuleResult.Success(
            RuleName,
            $"Ritmo e velocidade dentro dos padrões fisiológicos normais. Média: {avgSpeedKmh:F1} km/h (Pace: {pace}), Máxima: {maxPointSpeedKmh:F1} km/h.",
            avgSpeedKmh));
    }
}
