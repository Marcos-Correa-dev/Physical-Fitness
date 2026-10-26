using KmValidator.Domain.Enums;

namespace KmValidator.Domain.ValueObjects;

public sealed record HeartRate
{
    public int Bpm { get; }

    public HeartRate(int bpm)
    {
        if (bpm < 30 || bpm > 240)
            throw new ArgumentOutOfRangeException(nameof(bpm), "BPM deve estar em uma faixa fisiológica razoável (30 a 240).");

        Bpm = bpm;
    }

    public HeartRateZone GetZone(int maxHeartRate = 190)
    {
        if (maxHeartRate <= 0) maxHeartRate = 190;
        double pct = (double)Bpm / maxHeartRate;

        return pct switch
        {
            < 0.60 => HeartRateZone.Zone1Recovery,
            < 0.70 => HeartRateZone.Zone2Aerobic,
            < 0.80 => HeartRateZone.Zone3Tempo,
            < 0.90 => HeartRateZone.Zone4Threshold,
            _ => HeartRateZone.Zone5Anaerobic
        };
    }

    public override string ToString() => $"{Bpm} bpm";
}
