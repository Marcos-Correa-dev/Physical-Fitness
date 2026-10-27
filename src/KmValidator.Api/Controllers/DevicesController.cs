using KmValidator.Application.DTOs;
using KmValidator.Application.Mappers;
using KmValidator.Application.Ports;
using KmValidator.Application.Services;
using KmValidator.Domain.Entities;
using KmValidator.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace KmValidator.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DevicesController : ControllerBase
{
    private readonly IIngestActivityUseCase _ingestUseCase;
    private readonly ISimulateSmartwatchSyncUseCase _simulateUseCase;
    private readonly IWearableDeviceRepository _deviceRepository;
    private readonly IAthleteRepository _athleteRepository;
    private readonly IPhysicalActivityRepository _activityRepository;
    private readonly ActivityFileParserService _fileParserService;

    public DevicesController(
        IIngestActivityUseCase ingestUseCase,
        ISimulateSmartwatchSyncUseCase simulateUseCase,
        IWearableDeviceRepository deviceRepository,
        IAthleteRepository athleteRepository,
        IPhysicalActivityRepository activityRepository,
        ActivityFileParserService fileParserService)
    {
        _ingestUseCase = ingestUseCase;
        _simulateUseCase = simulateUseCase;
        _deviceRepository = deviceRepository;
        _athleteRepository = athleteRepository;
        _activityRepository = activityRepository;
        _fileParserService = fileParserService;
    }

    /// <summary>
    /// Consulta o histórico de atividades registradas para o relógio conectado no banco de dados.
    /// Puxa estritamente dados sincronizados do dispositivo via Bluetooth ou telemetria, SEM ler arquivos do computador/desktop.
    /// </summary>
    [HttpPost("sync-history")]
    [ProducesResponseType(typeof(SyncHistoryResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncWorkoutHistory([FromBody] SyncHistoryRequestDto? request, CancellationToken cancellationToken)
    {
        string deviceName = string.IsNullOrWhiteSpace(request?.DeviceName) ? "Xiaomi Mi Band 10" : request.DeviceName;

        var activities = await _activityRepository.GetAllRecentAsync(100, cancellationToken);
        var dtos = new List<ActivitySummaryDto>();

        foreach (var act in activities)
        {
            var device = await _deviceRepository.GetByIdAsync(act.DeviceId, cancellationToken);
            dtos.Add(ActivityMapper.ToDto(act, device?.DeviceName ?? deviceName));
        }

        var response = new SyncHistoryResponseDto
        {
            TotalFound = dtos.Count,
            SyncedCount = dtos.Count,
            NewCount = 0,
            AlreadyExistingCount = dtos.Count,
            Activities = dtos,
            Message = dtos.Count > 0
                ? $"{dtos.Count} atividade(s) sincronizada(s) do dispositivo {deviceName}."
                : $"Nenhuma atividade gravada ainda para o dispositivo {deviceName}."
        };

        return Ok(response);
    }

    /// <summary>
    /// Endpoint de ingestão/sincronização de telemetria emitido por relógios inteligentes (Garmin, Apple Watch, etc).
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ActivitySummaryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SyncDeviceTelemetry([FromBody] DeviceSyncTelemetryDto dto, CancellationToken cancellationToken)
    {
        var result = await _ingestUseCase.ExecuteAsync(dto, cancellationToken);
        return CreatedAtAction("GetById", "Activities", new { id = result.Id }, result);
    }

    /// <summary>
    /// Simula a transmissão de um treino por relógio eletrônico para teste imediato das regras de validação.
    /// </summary>
    [HttpPost("simulate")]
    [ProducesResponseType(typeof(ActivitySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SimulateDeviceSync([FromBody] SimulationRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _simulateUseCase.ExecuteAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lista os dispositivos vestíveis cadastrados no sistema.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WearableDevice>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevices(CancellationToken cancellationToken)
    {
        var athlete = await _athleteRepository.GetDefaultAthleteAsync(cancellationToken);
        if (athlete == null) return Ok(Array.Empty<WearableDevice>());

        var devices = await _deviceRepository.GetByAthleteIdAsync(athlete.Id, cancellationToken);
        return Ok(devices);
    }
}
