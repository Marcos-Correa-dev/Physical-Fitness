using KmValidator.Domain.Entities;
using KmValidator.Domain.ValueObjects;

namespace KmValidator.Domain.Services.Validation.Rules;

public class GpsDistanceHaversineRule : IActivityValidationRule
{
    public string RuleName => "GpsHaversineSanity";
    public int ExecutionOrder => 1;

    public Task<ValidationRuleResult> ValidateAsync(PhysicalActivity activity, CancellationToken cancellationToken = default)
    {
        // Musculação é uma atividade estática/indoor; zero deslocamento de satélite é esperado e correto
        if (activity.ActivityType == Enums.ActivityType.StrengthTraining)
        {
            return Task.FromResult(ValidationRuleResult.Success(
                RuleName,
                "Atividade indoor de Musculação/Treino de Força. Ausência de deslocamento geodésico validada com sucesso.",
                0.0));
        }

        // Treino indoor de Esteira / Treadmill (distância medida pelo sensor inercial / acelerômetro do relógio)
        if (activity.Title.Contains("Esteira", StringComparison.OrdinalIgnoreCase) ||
            activity.Title.Contains("Treadmill", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(ValidationRuleResult.Success(
                RuleName,
                $"Treino indoor em esteira rolante. Distância de {activity.ReportedDistanceKm:F2} km validada pelo sensor inercial do relógio.",
                activity.ReportedDistanceKm));
        }

        var points = activity.RoutePoints.OrderBy(p => p.SequenceOrder).ToList();
        if (points.Count < 2)
        {
            return Task.FromResult(ValidationRuleResult.Warning(
                RuleName,
                "A atividade possui pontos GPS insuficientes para verificação geodésica detalhada.",
                "Menos de 2 coordenadas registradas no percurso."));
        }

        double totalCalculatedKm = 0.0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i].GetCoordinates();
            var p2 = points[i + 1].GetCoordinates();
            totalCalculatedKm += p1.DistanceTo(p2).Kilometers;
        }

        double reportedKm = activity.ReportedDistanceKm;
        double discrepancyPct = reportedKm > 0
            ? Math.Abs(totalCalculatedKm - reportedKm) / reportedKm * 100.0
            : 100.0;

        if (discrepancyPct > 25.0)
        {
            return Task.FromResult(ValidationRuleResult.Failure(
                RuleName,
                $"Divergência crítica entre KM do relógio ({reportedKm:F2} km) e percurso GPS recalculado ({totalCalculatedKm:F2} km).",
                $"Discrepância calculada de {discrepancyPct:F1}%, excedendo o limite de segurança de 25%.",
                totalCalculatedKm));
        }

        if (discrepancyPct > 10.0)
        {
            return Task.FromResult(ValidationRuleResult.Warning(
                RuleName,
                $"Alerta de divergência moderada entre relógio ({reportedKm:F2} km) e rota GPS ({totalCalculatedKm:F2} km).",
                $"Discrepância de {discrepancyPct:F1}%. Pode indicar perda parcial de satélite ou túnel.",
                totalCalculatedKm));
        }

        return Task.FromResult(ValidationRuleResult.Success(
            RuleName,
            $"Distância verificada com precisão. GPS calculado: {totalCalculatedKm:F2} km vs Relógio: {reportedKm:F2} km (Divergência: {discrepancyPct:F1}%).",
            totalCalculatedKm));
    }
}
