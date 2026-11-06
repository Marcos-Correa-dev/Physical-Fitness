using System.Text;
using FluentAssertions;
using KmValidator.Application.DTOs;
using KmValidator.Application.Ports;
using KmValidator.Application.UseCases;
using KmValidator.Domain.Enums;
using Xunit;

namespace KmValidator.UnitTests;

public class ImportActivityFileUseCaseTests
{
    private class FakeIngestActivityUseCase : IIngestActivityUseCase
    {
        public DeviceSyncTelemetryDto? LastIngestedDto { get; private set; }

        public Task<ActivitySummaryDto> ExecuteAsync(DeviceSyncTelemetryDto dto, CancellationToken cancellationToken = default)
        {
            LastIngestedDto = dto;
            return Task.FromResult(new ActivitySummaryDto
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Status = "Valid",
                ReportedDistanceKm = dto.ReportedDistanceKm
            });
        }
    }

    [Fact]
    public async Task ExecuteAsync_ValidGpx_ShouldExtractCoordinatesAndIngest()
    {
        // Arrange
        var gpxXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<gpx version=""1.1"" creator=""StravaGPX"" xmlns=""http://www.topografix.com/GPX/1/1"" xmlns:gpxtpx=""http://www.garmin.com/xmlschemas/TrackPointExtension/v1"">
  <trk>
    <name>Corrida Ibirapuera 5k</name>
    <type>running</type>
    <trkseg>
      <trkpt lat=""-23.587416"" lon=""-46.657634"">
        <ele>760.0</ele>
        <time>2026-10-27T08:00:00Z</time>
        <extensions>
          <gpxtpx:TrackPointExtension>
            <gpxtpx:hr>145</gpxtpx:hr>
          </gpxtpx:TrackPointExtension>
        </extensions>
      </trkpt>
      <trkpt lat=""-23.588416"" lon=""-46.658634"">
        <ele>761.5</ele>
        <time>2026-10-27T08:01:00Z</time>
        <extensions>
          <gpxtpx:TrackPointExtension>
            <gpxtpx:hr>152</gpxtpx:hr>
          </gpxtpx:TrackPointExtension>
        </extensions>
      </trkpt>
    </trkseg>
  </trk>
</gpx>";

        var fakeIngest = new FakeIngestActivityUseCase();
        var useCase = new ImportActivityFileUseCase(fakeIngest);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gpxXml));

        // Act
        var result = await useCase.ExecuteAsync(stream, "ibirapuera.gpx", "Xiaomi Mi Band 10");

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Corrida Ibirapuera 5k");

        var captured = fakeIngest.LastIngestedDto;
        captured.Should().NotBeNull();
        captured!.ActivityType.Should().Be(ActivityType.Running);
        captured.GpsPoints.Should().HaveCount(2);
        captured.GpsPoints[0].Latitude.Should().BeApproximately(-23.587416, 0.00001);
        captured.GpsPoints[0].HeartRateBpm.Should().Be(145);
        captured.GpsPoints[1].HeartRateBpm.Should().Be(152);
    }

    [Fact]
    public async Task ExecuteAsync_GpxWithoutHeartRate_ShouldApplySensibleFallback()
    {
        // Arrange
        var gpxXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<gpx version=""1.1"" creator=""GenericGPS"">
  <trk>
    <name>Caminhada no Parque</name>
    <type>walking</type>
    <trkseg>
      <trkpt lat=""-23.550520"" lon=""-46.633308"">
        <ele>750.0</ele>
        <time>2026-10-27T09:00:00Z</time>
      </trkpt>
      <trkpt lat=""-23.551520"" lon=""-46.634308"">
        <ele>751.0</ele>
        <time>2026-10-27T09:02:00Z</time>
      </trkpt>
    </trkseg>
  </trk>
</gpx>";

        var fakeIngest = new FakeIngestActivityUseCase();
        var useCase = new ImportActivityFileUseCase(fakeIngest);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(gpxXml));

        // Act
        await useCase.ExecuteAsync(stream, "caminhada.gpx", "Garmin Forerunner");

        // Assert
        var captured = fakeIngest.LastIngestedDto;
        captured.Should().NotBeNull();
        captured!.ActivityType.Should().Be(ActivityType.Walking);
        captured.AvgHeartRateBpm.Should().BeGreaterThan(90); // Fallback fisiológico seguro
    }

    [Fact]
    public async Task ExecuteAsync_EmptyGpx_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var emptyGpx = @"<?xml version=""1.0"" encoding=""UTF-8""?><gpx version=""1.1""><trk><name>Vazio</name></trk></gpx>";
        var fakeIngest = new FakeIngestActivityUseCase();
        var useCase = new ImportActivityFileUseCase(fakeIngest);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(emptyGpx));

        // Act
        Func<Task> act = async () => await useCase.ExecuteAsync(stream, "empty.gpx", "Xiaomi Mi Band 10");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não contém pontos de coordenadas*");
    }
}
