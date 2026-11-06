using KmValidator.Domain.Enums;
using KmValidator.Domain.ValueObjects;

namespace KmValidator.Domain.Entities;

public class PhysicalActivity
{
    private readonly List<RoutePoint> _routePoints = new();

    public Guid Id { get; private set; }
    public Guid AthleteId { get; private set; }
    public Guid DeviceId { get; private set; }
    public ActivityType ActivityType { get; private set; }
    public string Title { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public int DurationSeconds { get; private set; }
    public double ReportedDistanceKm { get; private set; }
    public double ValidatedDistanceKm { get; private set; }
    public int CaloriesBurned { get; private set; }
    public int AverageHeartRateBpm { get; private set; }
    public int MaxHeartRateBpm { get; private set; }
    public ValidationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Relacionamentos DDD
    public IReadOnlyCollection<RoutePoint> RoutePoints => _routePoints.OrderBy(p => p.SequenceOrder).ToList().AsReadOnly();
    public ActivityValidation? Validation { get; private set; }

    // Ef Core
    protected PhysicalActivity()
    {
        Title = string.Empty;
    }

    public PhysicalActivity(
        Guid athleteId,
        Guid deviceId,
        ActivityType activityType,
        string title,
        DateTime startTime,
        DateTime endTime,
        double reportedDistanceKm,
        int caloriesBurned,
        int avgHeartRateBpm,
        int maxHeartRateBpm)
    {
        if (endTime <= startTime)
            throw new ArgumentException("Horário de término deve ser posterior ao horário de início.", nameof(endTime));

        if (reportedDistanceKm < 0)
            throw new ArgumentOutOfRangeException(nameof(reportedDistanceKm), "Distância reportada não pode ser negativa.");

        Id = Guid.NewGuid();
        AthleteId = athleteId;
        DeviceId = deviceId;
        ActivityType = activityType;
        Title = string.IsNullOrWhiteSpace(title) ? $"{activityType} Treino" : title.Trim();
        StartTime = startTime;
        EndTime = endTime;
        DurationSeconds = (int)(endTime - startTime).TotalSeconds;
        ReportedDistanceKm = Math.Round(reportedDistanceKm, 3);
        ValidatedDistanceKm = 0.0;
        CaloriesBurned = Math.Max(0, caloriesBurned);
        AverageHeartRateBpm = Math.Clamp(avgHeartRateBpm, 30, 240);
        MaxHeartRateBpm = Math.Clamp(maxHeartRateBpm, avgHeartRateBpm, 240);
        Status = ValidationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddRoutePoint(double latitude, double longitude, double? elevation, DateTime timestamp, double instantSpeedKmh, int heartRateBpm)
    {
        int sequence = _routePoints.Count + 1;
        var point = new RoutePoint(Id, sequence, latitude, longitude, elevation, timestamp, instantSpeedKmh, heartRateBpm);
        _routePoints.Add(point);
    }

    public void AddRoutePoints(IEnumerable<RoutePoint> points)
    {
        _routePoints.AddRange(points);
    }

    public void ApplyValidation(ActivityValidation validation, double approvedKm)
    {
        ArgumentNullException.ThrowIfNull(validation);

        Validation = validation;
        Status = validation.Status;
        ValidatedDistanceKm = Math.Round(approvedKm, 3);
    }

    public Distance GetReportedDistance() => Distance.FromKilometers(ReportedDistanceKm);
    public Distance GetValidatedDistance() => Distance.FromKilometers(ValidatedDistanceKm);
    public TimeSpan GetDuration() => TimeSpan.FromSeconds(DurationSeconds);
    public Pace GetPace() => Pace.FromDurationAndDistance(GetDuration(), GetReportedDistance());
}
