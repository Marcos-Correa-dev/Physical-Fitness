using KmValidator.Domain.ValueObjects;

namespace KmValidator.Domain.Entities;

public class RoutePoint
{
    public long Id { get; private set; }
    public Guid PhysicalActivityId { get; private set; }
    public int SequenceOrder { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double? ElevationMeters { get; private set; }
    public DateTime Timestamp { get; private set; }
    public double InstantSpeedKmh { get; private set; }
    public int HeartRateBpm { get; private set; }

    // Ef Core
    protected RoutePoint() { }

    public RoutePoint(
        Guid physicalActivityId,
        int sequenceOrder,
        double latitude,
        double longitude,
        double? elevationMeters,
        DateTime timestamp,
        double instantSpeedKmh,
        int heartRateBpm)
    {
        PhysicalActivityId = physicalActivityId;
        SequenceOrder = sequenceOrder;
        Latitude = latitude;
        Longitude = longitude;
        ElevationMeters = elevationMeters;
        Timestamp = timestamp;
        InstantSpeedKmh = Math.Max(0.0, instantSpeedKmh);
        HeartRateBpm = Math.Clamp(heartRateBpm, 30, 240);
    }

    public Coordinates GetCoordinates() => new(Latitude, Longitude, ElevationMeters);
}
