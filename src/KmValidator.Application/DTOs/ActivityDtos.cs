using KmValidator.Domain.Enums;

namespace KmValidator.Application.DTOs;

public class TelemetryPointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? ElevationMeters { get; set; }
    public DateTime TimestampUtc { get; set; }
    public double InstantSpeedKmh { get; set; }
    public int HeartRateBpm { get; set; }
}

public class DeviceSyncTelemetryDto
{
    public string DeviceSerialNumber { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = "Smartwatch Eletrônico";
    public string Manufacturer { get; set; } = "Garmin";
    public DeviceType DeviceType { get; set; } = DeviceType.Smartwatch;

    public ActivityType ActivityType { get; set; } = ActivityType.Running;
    public string Title { get; set; } = "Treino Sincronizado";
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public double ReportedDistanceKm { get; set; }
    public int? CaloriesBurned { get; set; }
    public int AvgHeartRateBpm { get; set; }
    public int MaxHeartRateBpm { get; set; }

    public List<TelemetryPointDto> GpsPoints { get; set; } = new();
}

public class ValidationIssueDto
{
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class ActivitySummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double ReportedDistanceKm { get; set; }
    public double ValidatedDistanceKm { get; set; }
    public double CalculatedGpsDistanceKm { get; set; }
    public double DiscrepancyPercentage { get; set; }
    public string FormattedDuration { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string FormattedDate { get; set; } = string.Empty;
    public string FormattedTime { get; set; } = string.Empty;
    public string DayOfWeek { get; set; } = string.Empty;
    public string FormattedPace { get; set; } = string.Empty;
    public double AverageSpeedKmh { get; set; }
    public int CaloriesBurned { get; set; }
    public int AverageHeartRateBpm { get; set; }
    public int MaxHeartRateBpm { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string ValidationSummary { get; set; } = string.Empty;
    public List<ValidationIssueDto> Issues { get; set; } = new();
    public List<TelemetryPointDto> RoutePoints { get; set; } = new();
}

public class DashboardMetricsDto
{
    public DateTime GeneratedAt { get; set; }
    public string CurrentFormattedDate { get; set; } = string.Empty;
    public string CurrentFormattedTime { get; set; } = string.Empty;
    public string CurrentDayOfWeek { get; set; } = string.Empty;

    public double TotalReportedKm { get; set; }
    public double TotalValidatedKm { get; set; }
    public int TotalCaloriesBurned { get; set; }
    public int TotalActivitiesCount { get; set; }
    public int ValidActivitiesCount { get; set; }
    public int SuspectActivitiesCount { get; set; }
    public int RejectedActivitiesCount { get; set; }
    public int OverallAvgHeartRate { get; set; }

    public List<ActivitySummaryDto> RecentActivities { get; set; } = new();
}

public class SimulationRequestDto
{
    public string Scenario { get; set; } = "valid_5k_run"; 
    // Options: "valid_5k_run", "valid_cycling", "fraud_vehicle_run", "gps_glitch", "abnormal_resting_hr"
    public string DeviceModel { get; set; } = "Garmin Forerunner 965";
}
