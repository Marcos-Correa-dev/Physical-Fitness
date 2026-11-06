using KmValidator.Domain.Enums;

namespace KmValidator.Domain.Entities;

public class WearableDevice
{
    public Guid Id { get; private set; }
    public Guid AthleteId { get; private set; }
    public string DeviceName { get; private set; }
    public string Manufacturer { get; private set; }
    public string SerialNumber { get; private set; }
    public DeviceType DeviceType { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public DateTime RegisteredAt { get; private set; }

    // Ef Core
    protected WearableDevice()
    {
        DeviceName = string.Empty;
        Manufacturer = string.Empty;
        SerialNumber = string.Empty;
    }

    public WearableDevice(
        Guid athleteId,
        string deviceName,
        string manufacturer,
        string serialNumber,
        DeviceType deviceType = DeviceType.Smartwatch)
    {
        Id = Guid.NewGuid();
        AthleteId = athleteId;
        DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Relógio Inteligente" : deviceName.Trim();
        Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? "Genérico" : manufacturer.Trim();
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : serialNumber.Trim();
        DeviceType = deviceType;
        IsActive = true;
        RegisteredAt = DateTime.UtcNow;
    }

    public void RecordSync()
    {
        LastSyncAt = DateTime.UtcNow;
    }
}
