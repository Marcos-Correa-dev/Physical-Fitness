using KmValidator.Domain.Entities;
using KmValidator.Domain.ValueObjects;

namespace KmValidator.Domain.Services.Validation.Rules;

public class GpsJitterTeleportationRule : IActivityValidationRule
{
    public string RuleName => "GpsJitterTeleportation";
    public int ExecutionOrder => 4;

    public Task<ValidationRuleResult> ValidateAsync(PhysicalActivity activity, CancellationToken cancellationToken = default)
    {
        var points = activity.RoutePoints.OrderBy(p => p.SequenceOrder).ToList();
        if (points.Count < 2)
        {
            return Task.FromResult(ValidationRuleResult.Success(RuleName, "Pontos insuficientes para teste de teleportação."));
        }

        int impossibleJumpsCount = 0;
        double maxJumpSpeedKmh = 0.0;

        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];

            double secondsDiff = (p2.Timestamp - p1.Timestamp).TotalSeconds;
            if (secondsDiff <= 0) continue;

            var coord1 = p1.GetCoordinates();
            var coord2 = p2.GetCoordinates();
            double distanceKm = coord1.DistanceTo(coord2).Kilometers;
            double segmentSpeedKmh = (distanceKm / secondsDiff) * 3600.0;

            if (segmentSpeedKmh > maxJumpSpeedKmh)
            {
                maxJumpSpeedKmh = segmentSpeedKmh;
            }

            // Se entre dois pontos subsequentes a velocidade exceder 120 km/h, houve teletransporte/perda grave de GPS
            if (segmentSpeedKmh > 120.0 && distanceKm > 0.1)
            {
                impossibleJumpsCount++;
            }
        }

        if (impossibleJumpsCount > 0)
        {
            return Task.FromResult(ValidationRuleResult.Failure(
                RuleName,
                $"Detectados {impossibleJumpsCount} saltos geográficos anômalos (teletransporte) no trajeto do relógio.",
                $"Velocidade instantânea de salto alcançou {maxJumpSpeedKmh:F1} km/h entre pontos consecutivos.",
                maxJumpSpeedKmh));
        }

        return Task.FromResult(ValidationRuleResult.Success(
            RuleName,
            $"Continuidade da trajetória sem saltos anômalos de satélite (Velocidade máxima de segmento: {maxJumpSpeedKmh:F1} km/h).",
            maxJumpSpeedKmh));
    }
}
