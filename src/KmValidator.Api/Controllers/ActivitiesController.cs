using KmValidator.Application.DTOs;
using KmValidator.Application.Mappers;
using KmValidator.Application.Ports;
using KmValidator.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace KmValidator.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ActivitiesController : ControllerBase
{
    private readonly IPhysicalActivityRepository _activityRepository;
    private readonly IWearableDeviceRepository _deviceRepository;
    private readonly IValidateActivityUseCase _validateUseCase;
    private readonly IUnitOfWork _unitOfWork;

    public ActivitiesController(
        IPhysicalActivityRepository activityRepository,
        IWearableDeviceRepository deviceRepository,
        IValidateActivityUseCase validateUseCase,
        IUnitOfWork unitOfWork)
    {
        _activityRepository = activityRepository;
        _deviceRepository = deviceRepository;
        _validateUseCase = validateUseCase;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Lista as atividades físicas mais recentes com paginação/limite.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ActivitySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRecent([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var activities = await _activityRepository.GetAllRecentAsync(limit, cancellationToken);
        var dtos = new List<ActivitySummaryDto>();

        foreach (var act in activities)
        {
            var device = await _deviceRepository.GetByIdAsync(act.DeviceId, cancellationToken);
            dtos.Add(ActivityMapper.ToDto(act, device?.DeviceName));
        }

        return Ok(dtos);
    }

    /// <summary>
    /// Obtém detalhes completos de uma atividade física, incluindo percurso GPS e auditoria de validação.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ActivitySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var activity = await _activityRepository.GetByIdAsync(id, cancellationToken);
        if (activity == null) return NotFound(new { message = $"Atividade {id} não encontrada." });

        var device = await _deviceRepository.GetByIdAsync(activity.DeviceId, cancellationToken);
        return Ok(ActivityMapper.ToDto(activity, device?.DeviceName));
    }

    /// <summary>
    /// Reexecuta manualmente as regras de validação algorítmica para a atividade especificada.
    /// </summary>
    [HttpPost("{id:guid}/validate")]
    [ProducesResponseType(typeof(ActivitySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revalidate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _validateUseCase.ExecuteAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Atividade {id} não encontrada." });
        }
    }

    /// <summary>
    /// Exclui uma atividade física e seus pontos de percurso associados.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var activity = await _activityRepository.GetByIdAsync(id, cancellationToken);
        if (activity == null) return NotFound(new { message = $"Atividade {id} não encontrada." });

        await _activityRepository.DeleteAsync(activity, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return NoContent();
    }
}
