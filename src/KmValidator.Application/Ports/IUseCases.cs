using KmValidator.Application.DTOs;

namespace KmValidator.Application.Ports;

public interface IIngestActivityUseCase
{
    Task<ActivitySummaryDto> ExecuteAsync(DeviceSyncTelemetryDto dto, CancellationToken cancellationToken = default);
}

public interface IValidateActivityUseCase
{
    Task<ActivitySummaryDto> ExecuteAsync(Guid activityId, CancellationToken cancellationToken = default);
}

public interface IGetDashboardMetricsUseCase
{
    Task<DashboardMetricsDto> ExecuteAsync(CancellationToken cancellationToken = default);
}

public interface ISimulateSmartwatchSyncUseCase
{
    Task<ActivitySummaryDto> ExecuteAsync(SimulationRequestDto request, CancellationToken cancellationToken = default);
}
