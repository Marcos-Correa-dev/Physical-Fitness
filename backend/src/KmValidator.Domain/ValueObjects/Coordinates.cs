namespace KmValidator.Domain.ValueObjects;

public sealed record Coordinates
{
    private const double EarthRadiusKm = 6371.0;

    public double Latitude { get; }
    public double Longitude { get; }
    public double? ElevationMeters { get; }

    public Coordinates(double latitude, double longitude, double? elevationMeters = null)
    {
        if (latitude < -90.0 || latitude > 90.0)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude deve estar entre -90 e 90 graus.");

        if (longitude < -180.0 || longitude > 180.0)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude deve estar entre -180 e 180 graus.");

        Latitude = latitude;
        Longitude = longitude;
        ElevationMeters = elevationMeters;
    }

    /// <summary>
    /// Calcula a distância geodésica em quilômetros até outro ponto usando a fórmula de Haversine.
    /// </summary>
    public Distance DistanceTo(Coordinates other)
    {
        ArgumentNullException.ThrowIfNull(other);

        double dLat = ToRadians(other.Latitude - Latitude);
        double dLon = ToRadians(other.Longitude - Longitude);

        double lat1Rad = ToRadians(Latitude);
        double lat2Rad = ToRadians(other.Latitude);

        double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                   Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0) * Math.Cos(lat1Rad) * Math.Cos(lat2Rad);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        double distanceKm = EarthRadiusKm * c;

        return Distance.FromKilometers(distanceKm);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    public override string ToString() => $"Lat: {Latitude:F6}, Lon: {Longitude:F6}";
}
