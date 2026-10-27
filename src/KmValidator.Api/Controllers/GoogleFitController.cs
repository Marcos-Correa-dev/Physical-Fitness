using KmValidator.Application.DTOs;
using KmValidator.Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace KmValidator.Api.Controllers;

[ApiController]
[Route("api/v1/integrations/google-fit")]
[Produces("application/json")]
public class GoogleFitController : ControllerBase
{
    private readonly IGoogleFitService _googleFitService;

    public GoogleFitController(IGoogleFitService googleFitService)
    {
        _googleFitService = googleFitService;
    }

    /// <summary>
    /// Retorna o status da integração com a API Google Fit / Health Connect (Android).
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(GoogleFitStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _googleFitService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Gera a URL de consentimento OAuth 2.0 do Google Fit para vincular a conta Google à aplicação.
    /// </summary>
    [HttpGet("auth-url")]
    [ProducesResponseType(typeof(GoogleFitAuthUrlResponseDto), StatusCodes.Status200OK)]
    public IActionResult GetAuthUrl([FromQuery] string? redirectUri)
    {
        var result = _googleFitService.GetAuthUrl(redirectUri);
        return Ok(result);
    }

    /// <summary>
    /// Sincroniza sessões de treino gravadas pelo Mi Fitness e repassadas ao Google Fit / Health Connect.
    /// Funciona com Bearer Access Token ou no modo Telemetria Nativa da Mi Band 10.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(GoogleFitSyncResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync([FromBody] GoogleFitSyncRequestDto? request, CancellationToken cancellationToken)
    {
        request ??= new GoogleFitSyncRequestDto();
        var response = await _googleFitService.SyncSessionsAsync(request, cancellationToken);
        return Ok(response);
    }
}
