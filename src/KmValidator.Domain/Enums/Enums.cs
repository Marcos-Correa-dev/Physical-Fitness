namespace KmValidator.Domain.Enums;

public enum ActivityType
{
    Running = 1,
    Walking = 2,
    Cycling = 3,
    Hiking = 4,
    StrengthTraining = 5 // Musculação / Treino de Força
}

public enum ValidationStatus
{
    Pending = 1,
    Valid = 2,
    Suspect = 3,
    Rejected = 4
}

public enum DeviceType
{
    Smartwatch = 1,
    GpsBikeComputer = 2,
    FitnessTracker = 3,
    SmartBand = 4 // Mi Band, pulseiras esportivas
}

public enum IssueSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}

public enum HeartRateZone
{
    Zone1Recovery = 1,     // 50-60% Max HR
    Zone2Aerobic = 2,      // 60-70% Max HR
    Zone3Tempo = 3,        // 70-80% Max HR
    Zone4Threshold = 4,    // 80-90% Max HR
    Zone5Anaerobic = 5     // 90-100% Max HR
}
