using KmValidator.Domain.Entities;
using KmValidator.Domain.Enums;

namespace KmValidator.Domain.Services.Validation.Rules;

public class BiometricEffortCorrelationRule : IActivityValidationRule
{
    public string RuleName => "BiometricEffortCorrelation";
    public int ExecutionOrder => 3;

    public Task<ValidationRuleResult> ValidateAsync(PhysicalActivity activity, CancellationToken cancellationToken = default)
    {
        int avgHr = activity.AverageHeartRateBpm;
        int maxHr = activity.MaxHeartRateBpm;
        double durationMinutes = activity.GetDuration().TotalMinutes;

        if (avgHr < 40)
        {
            return Task.FromResult(ValidationRuleResult.Failure(
                RuleName,
                $"Batimentos cardíacos médios ({avgHr} bpm) abaixo do limiar fisiológico mínimo para atividade física.",
                "Dispositivo relógio não registrou contato cutâneo confiável ou dados simulados.",
                avgHr));
        }

        double speedKmh = durationMinutes > 0 ? (activity.ReportedDistanceKm / (durationMinutes / 60.0)) : 0;

        // Caso de Musculação: exige picos cardíacos condizentes com séries de esforço anaeróbico
        if (activity.ActivityType == ActivityType.StrengthTraining && avgHr < 75)
        {
            return Task.FromResult(ValidationRuleResult.Warning(
                RuleName,
                $"Batimentos médios ({avgHr} bpm) baixos para treino de musculação/hipertrofia.",
                "Sessão de força exige resposta cardiovascular moderada a intensa durante as séries.",
                avgHr));
        }

        // Caso clássico de veículo: velocidade alta de corrida (>12 km/h) com frequência de descanso (<75 bpm)
        if (activity.ActivityType == ActivityType.Running && speedKmh > 12.0 && avgHr < 80)
        {
            return Task.FromResult(ValidationRuleResult.Failure(
                RuleName,
                $"Anomalia biométrica detectada: Ritmo intenso ({speedKmh:F1} km/h) com frequência cardíaca de repouso ({avgHr} bpm).",
                "Ausência de resposta cardiovascular ao esforço mecânico alegado. Provável deslocamento em automóvel.",
                avgHr));
        }

        // Caminhada rápida com batimentos de pico perigosos
        if (maxHr > 215)
        {
            return Task.FromResult(ValidationRuleResult.Warning(
                RuleName,
                $"Frequência cardíaca máxima ({maxHr} bpm) atipicamente alta.",
                "Possível ruído óptico do sensor fotopletismográfico do relógio ou taquicardia.",
                maxHr));
        }

        return Task.FromResult(ValidationRuleResult.Success(
            RuleName,
            $"Correlação biométrica compatível com a carga de esforço. Média: {avgHr} bpm, Máxima: {maxHr} bpm.",
            avgHr));
    }
}
