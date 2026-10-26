using System.Globalization;
using KmValidator.Application.DTOs;
using KmValidator.Domain.Entities;
using KmValidator.Domain.Enums;

namespace KmValidator.Application.Mappers;

public static class ActivityMapper
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    public static ActivitySummaryDto ToDto(PhysicalActivity activity, string? deviceName = null)
    {
        var duration = activity.GetDuration();
        string formattedDuration = duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}h {duration.Minutes:D2}m {duration.Seconds:D2}s"
            : $"{duration.Minutes:D2}m {duration.Seconds:D2}s";

        double speedKmh = duration.TotalHours > 0
            ? Math.Round(activity.ReportedDistanceKm / duration.TotalHours, 1)
            : 0.0;

        var issues = activity.Validation?.Issues.Select(i => new ValidationIssueDto
        {
            RuleName = i.RuleName,
            Severity = i.Severity.ToString(),
            Message = i.Message,
            Details = i.Details
        }).ToList() ?? new List<ValidationIssueDto>();

        var routePoints = activity.RoutePoints.Select(p => new TelemetryPointDto
        {
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            ElevationMeters = p.ElevationMeters,
            TimestampUtc = p.Timestamp,
            InstantSpeedKmh = p.InstantSpeedKmh,
            HeartRateBpm = p.HeartRateBpm
        }).ToList();

        return new ActivitySummaryDto
        {
            Id = activity.Id,
            Title = activity.Title,
            ActivityType = MapActivityType(activity.ActivityType),
            Status = activity.Status.ToString(),
            ReportedDistanceKm = activity.ReportedDistanceKm,
            ValidatedDistanceKm = activity.ValidatedDistanceKm,
            CalculatedGpsDistanceKm = activity.Validation?.CalculatedGpsDistanceKm ?? activity.ReportedDistanceKm,
            DiscrepancyPercentage = activity.Validation?.DiscrepancyPercentage ?? 0.0,
            DurationSeconds = activity.DurationSeconds,
            FormattedDuration = formattedDuration,
            StartTime = activity.StartTime,
            EndTime = activity.EndTime,
            FormattedDate = activity.StartTime.ToString("dd/MM/yyyy", PtBrCulture),
            FormattedTime = activity.StartTime.ToString("HH:mm:ss", PtBrCulture),
            DayOfWeek = activity.StartTime.ToString("dddd", PtBrCulture),
            FormattedPace = activity.GetPace().ToPaceString(),
            AverageSpeedKmh = speedKmh,
            CaloriesBurned = activity.CaloriesBurned,
            AverageHeartRateBpm = activity.AverageHeartRateBpm,
            MaxHeartRateBpm = activity.MaxHeartRateBpm,
            DeviceName = deviceName ?? "Relógio Inteligente",
            ValidationSummary = activity.Validation?.SummaryNotes ?? "Aguardando validação",
            Issues = issues,
            RoutePoints = routePoints
        };
    }

    private static string MapActivityType(ActivityType type) => type switch
    {
        ActivityType.Running => "Corrida",
        ActivityType.Walking => "Caminhada",
        ActivityType.Cycling => "Ciclismo",
        ActivityType.Hiking => "Trilha",
        ActivityType.StrengthTraining => "Musculação",
        _ => "Atividade Física"
    };
}
