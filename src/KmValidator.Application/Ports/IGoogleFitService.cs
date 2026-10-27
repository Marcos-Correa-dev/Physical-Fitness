using KmValidator.Application.DTOs;

namespace KmValidator.Application.Ports;

public interface IGoogleFitService
{
    Task<GoogleFitSyncResponseDto> SyncSessionsAsync(GoogleFitSyncRequestDto request, CancellationToken cancellationToken = default);
    Task<GoogleFitStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    GoogleFitAuthUrlResponseDto GetAuthUrl(string? redirectUri = null);
}
