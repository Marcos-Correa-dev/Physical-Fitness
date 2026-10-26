using KmValidator.Domain.Entities;
using KmValidator.Domain.Enums;
using KmValidator.Domain.ValueObjects;

namespace KmValidator.Domain.Services.Validation;

public class ActivityKmValidationService
{
    private readonly IEnumerable<IActivityValidationRule> _rules;

    public ActivityKmValidationService(IEnumerable<IActivityValidationRule> rules)
    {
        _rules = rules.OrderBy(r => r.ExecutionOrder).ToList();
    }

    public async Task<ActivityValidation> ValidateActivityAsync(PhysicalActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        var issues = new List<ValidationIssue>();
        bool hasCritical = false;
        bool hasWarning = false;

        double calculatedGpsKm = 0.0;
        var points = activity.RoutePoints.OrderBy(p => p.SequenceOrder).ToList();
        if (points.Count >= 2)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                calculatedGpsKm += points[i].GetCoordinates().DistanceTo(points[i + 1].GetCoordinates()).Kilometers;
            }
        }
        else
        {
            calculatedGpsKm = activity.ReportedDistanceKm;
        }

        double discrepancyPct = activity.ReportedDistanceKm > 0
            ? Math.Abs(calculatedGpsKm - activity.ReportedDistanceKm) / activity.ReportedDistanceKm * 100.0
            : 0.0;

        double maxSpeedKmh = points.Any()
            ? points.Max(p => p.InstantSpeedKmh)
            : (activity.DurationSeconds > 0 ? (activity.ReportedDistanceKm / (activity.DurationSeconds / 3600.0)) : 0.0);

        double avgPaceMinKm = activity.GetPace().MinutesPerKilometer;

        foreach (var rule in _rules)
        {
            var result = await rule.ValidateAsync(activity, cancellationToken);
            if (!result.Passed)
            {
                issues.Add(result.ToIssue());
                if (result.Severity == IssueSeverity.Critical)
                {
                    hasCritical = true;
                }
                else if (result.Severity == IssueSeverity.Warning)
                {
                    hasWarning = true;
                }
            }
        }

        ValidationStatus finalStatus;
        double approvedKm;
        string summaryNotes;

        if (hasCritical)
        {
            finalStatus = ValidationStatus.Rejected;
            approvedKm = 0.0;
            summaryNotes = "Atividade rejeitada. Foram identificadas violações críticas de integridade, ritmo ou biometria.";
        }
        else if (hasWarning)
        {
            finalStatus = ValidationStatus.Suspect;
            // Se for suspeita, aprova a menor distância entre o GPS e o Relógio para segurança
            approvedKm = Math.Min(activity.ReportedDistanceKm, calculatedGpsKm);
            summaryNotes = "Atividade marcada como suspeita. Exige atenção para divergência de sinal de GPS ou esforço inconsistente.";
        }
        else
        {
            finalStatus = ValidationStatus.Valid;
            approvedKm = activity.ReportedDistanceKm;
            summaryNotes = activity.ActivityType == ActivityType.StrengthTraining
                ? "Sessão de Musculação 100% validada. Monitoramento cardíaco e gasto calórico compatíveis com treino de força."
                : "Atividade 100% validada. Telemetria do relógio e GPS condizentes com o esforço biológico.";
        }

        var validation = new ActivityValidation(
            activity.Id,
            finalStatus,
            calculatedGpsKm,
            discrepancyPct,
            maxSpeedKmh,
            avgPaceMinKm,
            summaryNotes,
            issues);

        activity.ApplyValidation(validation, approvedKm);
        return validation;
    }
}
