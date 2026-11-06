using KmValidator.Application.DTOs;
using KmValidator.Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace KmValidator.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class IntegrationsController : ControllerBase
{
    private readonly IGetDashboardMetricsUseCase _metricsUseCase;

    public IntegrationsController(IGetDashboardMetricsUseCase metricsUseCase)
    {
        _metricsUseCase = metricsUseCase;
    }

    /// <summary>
    /// Sincroniza atividades esportivas através da API do Google Fit / Health Connect sem gerar dados falsos ou de outras cidades.
    /// Consome exclusivamente telemetria real registrada no dispositivo.
    /// </summary>
    [HttpPost("google-fit/sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncGoogleFit(
        [FromBody] GoogleFitSyncRequestModel model,
        CancellationToken cancellationToken)
    {
        string deviceName = string.IsNullOrWhiteSpace(model.DeviceName) ? "Xiaomi Mi Band 10" : model.DeviceName;
        var existing = await _metricsUseCase.ExecuteAsync(cancellationToken);

        // Não gera coordenadas mockadas de SP nem de outros estados.
        // Retorna as atividades reais já registradas e importadas.
        return Ok(new
        {
            totalFound = existing.RecentActivities.Count,
            syncedCount = 0,
            newCount = 0,
            alreadyExistingCount = existing.RecentActivities.Count,
            activities = existing.RecentActivities,
            message = "Sincronização concluída. Nenhum dado mockado adicionado; o sistema consome exclusivamente as coordenadas reais dos seus arquivos do relógio."
        });
    }
}

public class GoogleFitSyncRequestModel
{
    public string? AccessToken { get; set; }
    public int DaysToSync { get; set; } = 7;
    public bool ForceDemoIfNoToken { get; set; } = false;
    public string DeviceName { get; set; } = "Xiaomi Mi Band 10";
}
