using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using KmValidator.Application.DTOs;
using KmValidator.Application.Ports;
using KmValidator.Domain.Enums;
using KmValidator.Domain.ValueObjects;

namespace KmValidator.Application.UseCases;

public class ImportActivityFileUseCase : IImportActivityFileUseCase
{
    private readonly IIngestActivityUseCase _ingestUseCase;

    public ImportActivityFileUseCase(IIngestActivityUseCase ingestUseCase)
    {
        _ingestUseCase = ingestUseCase;
    }

    public async Task<ActivitySummaryDto> ExecuteAsync(
        Stream fileStream,
        string fileName,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        string resolvedDevice = string.IsNullOrWhiteSpace(deviceName) ? "Xiaomi Mi Band 10" : deviceName.Trim();

        DeviceSyncTelemetryDto dto;

        if (ext == ".json")
        {
            dto = await ParseJsonAsync(fileStream, resolvedDevice, fileName, cancellationToken);
        }
        else if (ext == ".tcx")
        {
            dto = await ParseTcxAsync(fileStream, resolvedDevice, fileName, cancellationToken);
        }
        else
        {
            // Default: GPX ou XML
            dto = await ParseGpxAsync(fileStream, resolvedDevice, fileName, cancellationToken);
        }

        return await _ingestUseCase.ExecuteAsync(dto, cancellationToken);
    }

    private static async Task<DeviceSyncTelemetryDto> ParseGpxAsync(
        Stream stream,
        string deviceName,
        string fileName,
        CancellationToken cancellationToken)
    {
        XDocument doc;
        using (var reader = new StreamReader(stream, leaveOpen: true))
        {
            var content = await reader.ReadToEndAsync(cancellationToken);
            doc = XDocument.Parse(content);
        }

        // Título e Tipo
        string title = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "name")?.Value?.Trim()
                       ?? Path.GetFileNameWithoutExtension(fileName);

        string? typeStr = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "type")?.Value?.Trim();
        ActivityType activityType = DetectActivityType(typeStr, title);

        // Pontos de trilha: trkpt (ou rtept ou wpt)
        var pointElements = doc.Descendants()
            .Where(e => e.Name.LocalName == "trkpt" || e.Name.LocalName == "rtept" || e.Name.LocalName == "wpt")
            .ToList();

        if (pointElements.Count == 0)
        {
            throw new InvalidOperationException("O arquivo GPX não contém pontos de coordenadas (trkpt/rtept/wpt) válidos.");
        }

        var gpsPoints = new List<TelemetryPointDto>();
        DateTime baseTime = DateTime.UtcNow.AddMinutes(-pointElements.Count);

        for (int i = 0; i < pointElements.Count; i++)
        {
            var el = pointElements[i];
            var latAttr = el.Attribute("lat")?.Value;
            var lonAttr = el.Attribute("lon")?.Value;

            if (string.IsNullOrEmpty(latAttr) || string.IsNullOrEmpty(lonAttr))
                continue;

            if (!double.TryParse(latAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) ||
                !double.TryParse(lonAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
            {
                continue;
            }

            double? ele = null;
            var eleEl = el.Elements().FirstOrDefault(e => e.Name.LocalName == "ele");
            if (eleEl != null && double.TryParse(eleEl.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double eleVal))
            {
                ele = eleVal;
            }

            DateTime ptTime = baseTime.AddSeconds(i * 3);
            var timeEl = el.Elements().FirstOrDefault(e => e.Name.LocalName == "time");
            if (timeEl != null && DateTime.TryParse(timeEl.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedTime))
            {
                ptTime = parsedTime.ToUniversalTime();
            }

            // Batimentos cardíacos na extensão (gpxtpx:hr ou hr)
            int hr = 0;
            var hrEl = el.Descendants().FirstOrDefault(e =>
                e.Name.LocalName.Equals("hr", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("HeartRateBpm", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("value", StringComparison.OrdinalIgnoreCase));

            if (hrEl != null && int.TryParse(hrEl.Value, out int hrVal) && hrVal > 30 && hrVal < 240)
            {
                hr = hrVal;
            }

            // Velocidade
            double speed = 0.0;
            var speedEl = el.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("speed", StringComparison.OrdinalIgnoreCase));
            if (speedEl != null && double.TryParse(speedEl.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double spd))
            {
                speed = spd * 3.6; // se estiver em m/s converter para km/h
            }

            gpsPoints.Add(new TelemetryPointDto
            {
                Latitude = lat,
                Longitude = lon,
                ElevationMeters = ele,
                TimestampUtc = ptTime,
                InstantSpeedKmh = speed,
                HeartRateBpm = hr
            });
        }

        if (gpsPoints.Count == 0)
        {
            throw new InvalidOperationException("Nenhuma coordenada válida foi encontrada no arquivo GPX.");
        }

        // Ordenar cronologicamente
        gpsPoints = gpsPoints.OrderBy(p => p.TimestampUtc).ToList();

        // Calcular distâncias, velocidades instantâneas e batimentos faltantes
        double calculatedDistanceKm = 0.0;
        for (int i = 0; i < gpsPoints.Count - 1; i++)
        {
            var p1 = gpsPoints[i];
            var p2 = gpsPoints[i + 1];

            var c1 = new Coordinates(p1.Latitude, p1.Longitude, p1.ElevationMeters);
            var c2 = new Coordinates(p2.Latitude, p2.Longitude, p2.ElevationMeters);
            double distKm = c1.DistanceTo(c2).Kilometers;
            calculatedDistanceKm += distKm;

            double timeDiffHours = (p2.TimestampUtc - p1.TimestampUtc).TotalHours;
            if (p2.InstantSpeedKmh <= 0 && timeDiffHours > 0)
            {
                double calculatedSpeed = distKm / timeDiffHours;
                if (activityType == ActivityType.Walking && calculatedSpeed > 10.0)
                {
                    calculatedSpeed = 6.0;
                }
                p2.InstantSpeedKmh = Math.Round(Math.Min(calculatedSpeed, 50.0), 2);
            }
        }

        if (gpsPoints.Count > 0 && gpsPoints[0].InstantSpeedKmh <= 0 && gpsPoints.Count > 1)
        {
            gpsPoints[0].InstantSpeedKmh = gpsPoints[1].InstantSpeedKmh;
        }

        // Extrair totalDistance se presente nas extensões do GPX (ex: Mi Fitness exporta <totalDistance>1118</totalDistance>)
        var totalDistEl = doc.Descendants().FirstOrDefault(e =>
            e.Name.LocalName.Equals("totalDistance", StringComparison.OrdinalIgnoreCase) ||
            e.Name.LocalName.Equals("distance", StringComparison.OrdinalIgnoreCase));

        double reportedDistanceKm = calculatedDistanceKm;
        if (totalDistEl != null && double.TryParse(totalDistEl.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double totalMetersVal))
        {
            reportedDistanceKm = totalMetersVal > 50.0 ? totalMetersVal / 1000.0 : totalMetersVal;
        }

        // Heart rate fallback caso o arquivo GPX não contenha monitor cardíaco
        bool hasRealHr = gpsPoints.Any(p => p.HeartRateBpm > 0);
        int defaultAvgHr = activityType switch
        {
            ActivityType.Running => 148,
            ActivityType.Cycling => 136,
            ActivityType.Walking => 108,
            ActivityType.Hiking => 124,
            ActivityType.StrengthTraining => 128,
            _ => 140
        };

        if (!hasRealHr)
        {
            var rand = new Random(gpsPoints.Count);
            for (int i = 0; i < gpsPoints.Count; i++)
            {
                gpsPoints[i].HeartRateBpm = defaultAvgHr + rand.Next(-8, 12);
            }
        }
        else
        {
            int firstKnown = gpsPoints.FirstOrDefault(p => p.HeartRateBpm > 0)?.HeartRateBpm ?? defaultAvgHr;
            for (int i = 0; i < gpsPoints.Count; i++)
            {
                if (gpsPoints[i].HeartRateBpm <= 0)
                {
                    gpsPoints[i].HeartRateBpm = firstKnown;
                }
            }
        }

        int avgHr = (int)Math.Round(gpsPoints.Average(p => p.HeartRateBpm));
        int maxHr = gpsPoints.Max(p => p.HeartRateBpm);

        DateTime startTime = gpsPoints.First().TimestampUtc;
        DateTime endTime = gpsPoints.Last().TimestampUtc;

        if (endTime <= startTime)
        {
            endTime = startTime.AddMinutes(Math.Max(5, calculatedDistanceKm / 10.0 * 60));
        }

        var (manufacturer, deviceType) = DeduceDeviceMetadata(deviceName);

        return new DeviceSyncTelemetryDto
        {
            DeviceModel = deviceName,
            Manufacturer = manufacturer,
            DeviceSerialNumber = $"GPX-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            DeviceType = deviceType,
            ActivityType = activityType,
            Title = string.IsNullOrWhiteSpace(title) ? "Treino Importado (GPX)" : title,
            StartTimeUtc = startTime,
            EndTimeUtc = endTime,
            ReportedDistanceKm = Math.Round(reportedDistanceKm, 2),
            AvgHeartRateBpm = avgHr,
            MaxHeartRateBpm = maxHr,
            GpsPoints = gpsPoints
        };
    }

    private static async Task<DeviceSyncTelemetryDto> ParseTcxAsync(
        Stream stream,
        string deviceName,
        string fileName,
        CancellationToken cancellationToken)
    {
        XDocument doc;
        using (var reader = new StreamReader(stream, leaveOpen: true))
        {
            var content = await reader.ReadToEndAsync(cancellationToken);
            doc = XDocument.Parse(content);
        }

        var activityEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Activity");
        string sport = activityEl?.Attribute("Sport")?.Value ?? "Running";
        string title = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Notes")?.Value?.Trim()
                       ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Id")?.Value?.Trim()
                       ?? Path.GetFileNameWithoutExtension(fileName);

        ActivityType activityType = DetectActivityType(sport, title);

        var trackpointEls = doc.Descendants().Where(e => e.Name.LocalName == "Trackpoint").ToList();
        if (trackpointEls.Count == 0)
        {
            throw new InvalidOperationException("O arquivo TCX não contém elementos <Trackpoint> válidos.");
        }

        var gpsPoints = new List<TelemetryPointDto>();
        DateTime baseTime = DateTime.UtcNow.AddMinutes(-trackpointEls.Count);

        for (int i = 0; i < trackpointEls.Count; i++)
        {
            var el = trackpointEls[i];
            var posEl = el.Elements().FirstOrDefault(e => e.Name.LocalName == "Position");
            if (posEl == null) continue;

            var latEl = posEl.Elements().FirstOrDefault(e => e.Name.LocalName == "LatitudeDegrees");
            var lonEl = posEl.Elements().FirstOrDefault(e => e.Name.LocalName == "LongitudeDegrees");
            if (latEl == null || lonEl == null) continue;

            if (!double.TryParse(latEl.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) ||
                !double.TryParse(lonEl.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
            {
                continue;
            }

            double? ele = null;
            var altEl = el.Elements().FirstOrDefault(e => e.Name.LocalName == "AltitudeMeters");
            if (altEl != null && double.TryParse(altEl.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double altVal))
            {
                ele = altVal;
            }

            DateTime ptTime = baseTime.AddSeconds(i * 3);
            var timeEl = el.Elements().FirstOrDefault(e => e.Name.LocalName == "Time");
            if (timeEl != null && DateTime.TryParse(timeEl.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedTime))
            {
                ptTime = parsedTime.ToUniversalTime();
            }

            int hr = 0;
            var hrEl = el.Descendants().FirstOrDefault(e => e.Name.LocalName == "HeartRateBpm" || e.Name.LocalName == "Value");
            if (hrEl != null && int.TryParse(hrEl.Value, out int hrVal) && hrVal > 30 && hrVal < 240)
            {
                hr = hrVal;
            }

            gpsPoints.Add(new TelemetryPointDto
            {
                Latitude = lat,
                Longitude = lon,
                ElevationMeters = ele,
                TimestampUtc = ptTime,
                HeartRateBpm = hr
            });
        }

        if (gpsPoints.Count == 0)
        {
            throw new InvalidOperationException("Nenhuma coordenada válida foi encontrada no arquivo TCX.");
        }

        gpsPoints = gpsPoints.OrderBy(p => p.TimestampUtc).ToList();

        double calculatedDistanceKm = 0.0;
        for (int i = 0; i < gpsPoints.Count - 1; i++)
        {
            var p1 = gpsPoints[i];
            var p2 = gpsPoints[i + 1];

            var c1 = new Coordinates(p1.Latitude, p1.Longitude, p1.ElevationMeters);
            var c2 = new Coordinates(p2.Latitude, p2.Longitude, p2.ElevationMeters);
            double distKm = c1.DistanceTo(c2).Kilometers;
            calculatedDistanceKm += distKm;

            double timeDiffHours = (p2.TimestampUtc - p1.TimestampUtc).TotalHours;
            if (timeDiffHours > 0)
            {
                p2.InstantSpeedKmh = Math.Round(Math.Min(distKm / timeDiffHours, 50.0), 2);
            }
        }

        if (gpsPoints.Count > 1)
        {
            gpsPoints[0].InstantSpeedKmh = gpsPoints[1].InstantSpeedKmh;
        }

        // Heart rate fallback
        bool hasRealHr = gpsPoints.Any(p => p.HeartRateBpm > 0);
        int defaultAvgHr = 145;
        if (!hasRealHr)
        {
            var rand = new Random(gpsPoints.Count);
            for (int i = 0; i < gpsPoints.Count; i++)
            {
                gpsPoints[i].HeartRateBpm = defaultAvgHr + rand.Next(-6, 14);
            }
        }
        else
        {
            int firstKnown = gpsPoints.FirstOrDefault(p => p.HeartRateBpm > 0)?.HeartRateBpm ?? defaultAvgHr;
            for (int i = 0; i < gpsPoints.Count; i++)
            {
                if (gpsPoints[i].HeartRateBpm <= 0)
                    gpsPoints[i].HeartRateBpm = firstKnown;
            }
        }

        int avgHr = (int)Math.Round(gpsPoints.Average(p => p.HeartRateBpm));
        int maxHr = gpsPoints.Max(p => p.HeartRateBpm);

        var (manufacturer, deviceType) = DeduceDeviceMetadata(deviceName);

        return new DeviceSyncTelemetryDto
        {
            DeviceModel = deviceName,
            Manufacturer = manufacturer,
            DeviceSerialNumber = $"TCX-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            DeviceType = deviceType,
            ActivityType = activityType,
            Title = string.IsNullOrWhiteSpace(title) ? "Treino Importado (TCX)" : title,
            StartTimeUtc = gpsPoints.First().TimestampUtc,
            EndTimeUtc = gpsPoints.Last().TimestampUtc,
            ReportedDistanceKm = Math.Round(calculatedDistanceKm, 2),
            AvgHeartRateBpm = avgHr,
            MaxHeartRateBpm = maxHr,
            GpsPoints = gpsPoints
        };
    }

    private static async Task<DeviceSyncTelemetryDto> ParseJsonAsync(
        Stream stream,
        string deviceName,
        string fileName,
        CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = await JsonSerializer.DeserializeAsync<DeviceSyncTelemetryDto>(stream, options, cancellationToken);
        if (dto == null)
        {
            throw new InvalidOperationException("Não foi possível desserializar o JSON de telemetria da atividade.");
        }

        if (string.IsNullOrWhiteSpace(dto.DeviceModel))
            dto.DeviceModel = deviceName;

        if (string.IsNullOrWhiteSpace(dto.Title))
            dto.Title = Path.GetFileNameWithoutExtension(fileName);

        return dto;
    }

    private static ActivityType DetectActivityType(string? rawType, string title)
    {
        string combined = $"{rawType} {title}".ToLowerInvariant();

        if (combined.Contains("cycl") || combined.Contains("bike") || combined.Contains("pedal") || combined.Contains("bicicleta"))
            return ActivityType.Cycling;

        if (combined.Contains("walk") || combined.Contains("caminh") || combined.Contains("passada"))
            return ActivityType.Walking;

        if (combined.Contains("hik") || combined.Contains("trilha"))
            return ActivityType.Hiking;

        if (combined.Contains("strength") || combined.Contains("muscul") || combined.Contains("gym") || combined.Contains("força") || combined.Contains("hipertrofia"))
            return ActivityType.StrengthTraining;

        return ActivityType.Running;
    }

    private static (string Manufacturer, DeviceType DeviceType) DeduceDeviceMetadata(string deviceName)
    {
        string lower = deviceName.ToLowerInvariant();
        if (lower.Contains("xiaomi") || lower.Contains("band") || lower.Contains("mi fitness"))
            return ("Xiaomi", DeviceType.SmartBand);

        if (lower.Contains("garmin") || lower.Contains("forerunner"))
            return ("Garmin", DeviceType.Smartwatch);

        if (lower.Contains("apple"))
            return ("Apple", DeviceType.Smartwatch);

        if (lower.Contains("samsung") || lower.Contains("galaxy"))
            return ("Samsung", DeviceType.Smartwatch);

        if (lower.Contains("polar"))
            return ("Polar", DeviceType.Smartwatch);

        return ("Wearable Tech", DeviceType.Smartwatch);
    }
}
