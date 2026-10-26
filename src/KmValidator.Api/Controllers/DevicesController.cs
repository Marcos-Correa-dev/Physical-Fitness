using KmValidator.Application.DTOs;
using KmValidator.Application.Ports;
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

    public DevicesController(
        IIngestActivityUseCase ingestUseCase,
        ISimulateSmartwatchSyncUseCase simulateUseCase,
        IWearableDeviceRepository deviceRepository,
        IAthleteRepository athleteRepository)
    {
        _ingestUseCase = ingestUseCase;
        _simulateUseCase = simulateUseCase;
        _deviceRepository = deviceRepository;
        _athleteRepository = athleteRepository;
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
