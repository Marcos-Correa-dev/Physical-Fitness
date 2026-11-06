namespace KmValidator.Domain.ValueObjects;

public sealed record Pace
{
    public double MinutesPerKilometer { get; }

    public Pace(double minutesPerKilometer)
    {
        if (minutesPerKilometer < 0)
            throw new ArgumentOutOfRangeException(nameof(minutesPerKilometer), "O ritmo (pace) não pode ser negativo.");

        MinutesPerKilometer = Math.Round(minutesPerKilometer, 2);
    }

    public static Pace FromSpeedKmh(double speedKmh)
    {
        if (speedKmh <= 0.0) return new Pace(0);
        return new Pace(60.0 / speedKmh);
    }

    public static Pace FromDurationAndDistance(TimeSpan duration, Distance distance)
    {
        if (distance.Kilometers <= 0.0) return new Pace(0);
        double totalMinutes = duration.TotalMinutes;
        return new Pace(totalMinutes / distance.Kilometers);
    }

    public double ToSpeedKmh()
    {
        if (MinutesPerKilometer <= 0.0) return 0.0;
        return Math.Round(60.0 / MinutesPerKilometer, 2);
    }

    public string ToPaceString()
    {
        if (MinutesPerKilometer <= 0.0) return "0:00 /km";
        int minutes = (int)MinutesPerKilometer;
        int seconds = (int)Math.Round((MinutesPerKilometer - minutes) * 60.0);
        if (seconds == 60)
        {
            minutes++;
            seconds = 0;
        }
        return $"{minutes}:{seconds:D2} /km";
    }

    public override string ToString() => ToPaceString();
}
