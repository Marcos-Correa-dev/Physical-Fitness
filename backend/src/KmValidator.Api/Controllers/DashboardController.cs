using KmValidator.Application.DTOs;
using KmValidator.Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace KmValidator.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IGetDashboardMetricsUseCase _dashboardMetricsUseCase;

    public DashboardController(IGetDashboardMetricsUseCase dashboardMetricsUseCase)
    {
        _dashboardMetricsUseCase = dashboardMetricsUseCase;
    }

    /// <summary>
    /// Retorna métricas consolidadas do dashboard (KM rodados, calorias, frequência cardíaca, status e atividades recentes).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var metrics = await _dashboardMetricsUseCase.ExecuteAsync(cancellationToken);
        return Ok(metrics);
    }
}
