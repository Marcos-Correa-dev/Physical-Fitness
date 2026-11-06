namespace KmValidator.Domain.ValueObjects;

public sealed record Distance
{
    public double Kilometers { get; }
    public double Meters => Kilometers * 1000.0;

    private Distance(double kilometers)
    {
        if (kilometers < 0)
            throw new ArgumentOutOfRangeException(nameof(kilometers), "A distância não pode ser negativa.");

        Kilometers = Math.Round(kilometers, 3);
    }

    public static Distance FromKilometers(double kilometers) => new(kilometers);
    public static Distance FromMeters(double meters) => new(meters / 1000.0);
    public static Distance Zero => new(0.0);

    public Distance Add(Distance other) => new(Kilometers + other.Kilometers);

    public override string ToString() => $"{Kilometers:0.00} km";
}
