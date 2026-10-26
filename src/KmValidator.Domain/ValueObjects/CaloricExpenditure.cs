using KmValidator.Domain.Enums;

namespace KmValidator.Domain.ValueObjects;

public sealed record CaloricExpenditure
{
    public int Kcal { get; }

    public CaloricExpenditure(int kcal)
    {
        if (kcal < 0)
            throw new ArgumentOutOfRangeException(nameof(kcal), "As calorias não podem ser negativas.");

        Kcal = kcal;
    }

    /// <summary>
    /// Estimativa fisiológica aproximada com base no peso, duração e MET (Equivalente Metabólico).
    /// </summary>
    public static CaloricExpenditure Estimate(ActivityType activityType, double weightKg, TimeSpan duration, double avgSpeedKmh)
    {
        if (weightKg <= 0) weightKg = 70.0; // peso médio padrão
        double hours = duration.TotalHours;

        double met = activityType switch
        {
            ActivityType.Running => avgSpeedKmh switch
            {
                < 8.0 => 8.0,
                < 10.0 => 9.8,
                < 12.0 => 11.5,
                < 14.0 => 12.8,
                _ => 14.5
            },
            ActivityType.Cycling => avgSpeedKmh switch
            {
                < 16.0 => 6.0,
                < 20.0 => 8.0,
                < 25.0 => 10.0,
                _ => 12.0
            },
            ActivityType.Walking => avgSpeedKmh < 5.0 ? 3.5 : 4.5,
            ActivityType.StrengthTraining => 6.5, // Musculação / Levantamento de Peso (alta demanda metabólica)
            _ => 5.0
        };

        // Fórmula: Calorias = MET * Peso(kg) * Duração(horas)
        int estimatedKcal = (int)Math.Round(met * weightKg * hours);
        return new CaloricExpenditure(Math.Max(estimatedKcal, 10));
    }

    public override string ToString() => $"{Kcal} kcal";
}
