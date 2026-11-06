namespace KmValidator.Domain.Entities;

public class Athlete
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public double WeightKg { get; private set; }
    public int MaxHeartRate { get; private set; }
    public int RestingHeartRate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Ef Core
    protected Athlete()
    {
        Name = string.Empty;
        Email = string.Empty;
    }

    public Athlete(string name, string email, double weightKg = 70.0, int maxHeartRate = 190, int restingHeartRate = 60)
    {
        Id = Guid.NewGuid();
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Nome é obrigatório.", nameof(name)) : name.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? throw new ArgumentException("Email é obrigatório.", nameof(email)) : email.Trim().ToLowerInvariant();
        WeightKg = weightKg > 0 ? weightKg : 70.0;
        MaxHeartRate = maxHeartRate > 100 ? maxHeartRate : 190;
        RestingHeartRate = restingHeartRate > 30 ? restingHeartRate : 60;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, double weightKg, int maxHeartRate, int restingHeartRate)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name.Trim();
        if (weightKg > 0) WeightKg = weightKg;
        if (maxHeartRate > 100) MaxHeartRate = maxHeartRate;
        if (restingHeartRate > 30) RestingHeartRate = restingHeartRate;
    }
}
