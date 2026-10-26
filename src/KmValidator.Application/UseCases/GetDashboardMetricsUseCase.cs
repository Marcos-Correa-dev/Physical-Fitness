using System.Globalization;
using KmValidator.Application.DTOs;
using KmValidator.Application.Mappers;
using KmValidator.Application.Ports;
using KmValidator.Domain.Enums;
using KmValidator.Domain.Ports;

namespace KmValidator.Application.UseCases;

public class GetDashboardMetricsUseCase : IGetDashboardMetricsUseCase
{
    private readonly IPhysicalActivityRepository _activityRepository;
    private readonly IWearableDeviceRepository _deviceRepository;
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    public GetDashboardMetricsUseCase(
        IPhysicalActivityRepository activityRepository,
        IWearableDeviceRepository deviceRepository)
    {
        _activityRepository = activityRepository;
        _deviceRepository = deviceRepository;
    }

    public async Task<DashboardMetricsDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var activities = await _activityRepository.GetAllRecentAsync(100, cancellationToken);

        // Agrupamento de dispositivos para cache de nomes
        var deviceIds = activities.Select(a => a.DeviceId).Distinct().ToList();
        var deviceDict = new Dictionary<Guid, string>();
        foreach (var devId in deviceIds)
        {
            var dev = await _deviceRepository.GetByIdAsync(devId, cancellationToken);
            if (dev != null)
            {
                deviceDict[devId] = $"{dev.Manufacturer} {dev.DeviceName}";
            }
        }

        var activityDtos = activities.Select(a =>
        {
            deviceDict.TryGetValue(a.DeviceId, out var devName);
            return ActivityMapper.ToDto(a, devName);
        }).ToList();

        double totalReportedKm = Math.Round(activities.Sum(a => a.ReportedDistanceKm), 2);
        double totalValidatedKm = Math.Round(activities.Sum(a => a.ValidatedDistanceKm), 2);
        int totalCalories = activities.Sum(a => a.CaloriesBurned);
        int validCount = activities.Count(a => a.Status == ValidationStatus.Valid);
        int suspectCount = activities.Count(a => a.Status == ValidationStatus.Suspect);
        int rejectedCount = activities.Count(a => a.Status == ValidationStatus.Rejected);
        int avgHr = activities.Any() ? (int)Math.Round(activities.Average(a => a.AverageHeartRateBpm)) : 0;

        return new DashboardMetricsDto
        {
            GeneratedAt = now,
            CurrentFormattedDate = now.ToString("dd/MM/yyyy", PtBrCulture),
            CurrentFormattedTime = now.ToString("HH:mm:ss", PtBrCulture),
            CurrentDayOfWeek = now.ToString("dddd", PtBrCulture),
            TotalReportedKm = totalReportedKm,
            TotalValidatedKm = totalValidatedKm,
            TotalCaloriesBurned = totalCalories,
            TotalActivitiesCount = activities.Count,
            ValidActivitiesCount = validCount,
            SuspectActivitiesCount = suspectCount,
            RejectedActivitiesCount = rejectedCount,
            OverallAvgHeartRate = avgHr,
            RecentActivities = activityDtos
        };
    }
}
