using KmValidator.Application.DTOs;
using KmValidator.Application.Mappers;
using KmValidator.Application.Ports;
using KmValidator.Domain.Entities;
using KmValidator.Domain.Ports;
using KmValidator.Domain.Services.Validation;
using KmValidator.Domain.ValueObjects;

namespace KmValidator.Application.UseCases;

public class IngestActivityUseCase : IIngestActivityUseCase
{
    private readonly IPhysicalActivityRepository _activityRepository;
    private readonly IAthleteRepository _athleteRepository;
    private readonly IWearableDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ActivityKmValidationService _validationService;

    public IngestActivityUseCase(
        IPhysicalActivityRepository activityRepository,
        IAthleteRepository athleteRepository,
        IWearableDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork,
        ActivityKmValidationService validationService)
    {
        _activityRepository = activityRepository;
        _athleteRepository = athleteRepository;
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
    }

    public async Task<ActivitySummaryDto> ExecuteAsync(DeviceSyncTelemetryDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // 1. Obter ou registrar Atleta padrão
        var athlete = await _athleteRepository.GetDefaultAthleteAsync(cancellationToken);
        if (athlete == null)
        {
            athlete = new Athlete("Marcos Corrêa", "marcos.atleta@example.com", 75.0, 192, 58);
            await _athleteRepository.AddAsync(athlete, cancellationToken);
        }

        // 2. Obter ou registrar Dispositivo Wearable (Relógio Inteligente)
        string serialNumber = string.IsNullOrWhiteSpace(dto.DeviceSerialNumber)
            ? "WATCH-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()
            : dto.DeviceSerialNumber.Trim();

        var device = await _deviceRepository.GetBySerialNumberAsync(serialNumber, cancellationToken);
        if (device == null)
        {
            device = new WearableDevice(athlete.Id, dto.DeviceModel, dto.Manufacturer, serialNumber, dto.DeviceType);
            await _deviceRepository.AddAsync(device, cancellationToken);
        }
        device.RecordSync();

        // Verificar se a atividade já foi importada anteriormente para evitar duplicidade
        var existingActivity = await _activityRepository.GetByStartTimeAsync(dto.StartTimeUtc, cancellationToken);
        if (existingActivity != null)
        {
            return ActivityMapper.ToDto(existingActivity, device.DeviceName);
        }

        // 3. Calcular calorias se não fornecidas
        int calories = dto.CaloriesBurned ?? CaloricExpenditure.Estimate(
            dto.ActivityType,
            athlete.WeightKg,
            dto.EndTimeUtc - dto.StartTimeUtc,
            (dto.EndTimeUtc - dto.StartTimeUtc).TotalHours > 0
                ? dto.ReportedDistanceKm / (dto.EndTimeUtc - dto.StartTimeUtc).TotalHours
                : 0.0
        ).Kcal;

        // 4. Criar Entidade Raiz de Atividade Física (DDD Aggregate)
        var activity = new PhysicalActivity(
            athlete.Id,
            device.Id,
            dto.ActivityType,
            dto.Title,
            dto.StartTimeUtc,
            dto.EndTimeUtc,
            dto.ReportedDistanceKm,
            calories,
            dto.AvgHeartRateBpm,
            dto.MaxHeartRateBpm);

        // 5. Adicionar pontos de percurso GPS
        if (dto.GpsPoints != null && dto.GpsPoints.Any())
        {
            foreach (var pt in dto.GpsPoints)
            {
                activity.AddRoutePoint(
                    pt.Latitude,
                    pt.Longitude,
                    pt.ElevationMeters,
                    pt.TimestampUtc,
                    pt.InstantSpeedKmh,
                    pt.HeartRateBpm);
            }
        }

        // 6. Executar Pipeline de Validação de KM (S.O.L.I.D. & Domain Service)
        await _validationService.ValidateActivityAsync(activity, cancellationToken);

        // 7. Persistir agregado e salvar transação
        await _activityRepository.AddAsync(activity, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ActivityMapper.ToDto(activity, device.DeviceName);
    }
}
