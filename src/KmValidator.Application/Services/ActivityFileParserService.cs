using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using KmValidator.Application.DTOs;
using KmValidator.Domain.Enums;
using KmValidator.Domain.ValueObjects;

namespace KmValidator.Application.Services;

public class ActivityFileParserService
{
    private static readonly XNamespace GpxNs = "http://www.topografix.com/GPX/1/1";
    private static readonly XNamespace TcxNs = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";

    /// <summary>
    /// Faz o parsing de arquivos GPX (exportados por Mi Fitness, Samsung Health, Garmin, Strava).
    /// </summary>
    public DeviceSyncTelemetryDto ParseGpx(Stream gpxStream, string fileName = "", string deviceName = "Xiaomi Mi Band 10 (Mi Fitness)")
    {
        var doc = XDocument.Load(gpxStream);
        var trk = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "trk");
        string xmlTitle = trk?.Element(trk.Name.Namespace + "name")?.Value ?? "";
        string typeStr = trk?.Element(trk.Name.Namespace + "type")?.Value?.ToLowerInvariant() ?? "";

        var trkpts = doc.Descendants().Where(e => e.Name.LocalName == "trkpt").ToList();
        bool hasGpsCoordinates = trkpts.Any();

        var activityType = DetermineActivityType(typeStr, fileName, hasGpsCoordinates);
        string finalTitle = !string.IsNullOrWhiteSpace(xmlTitle) && !xmlTitle.Contains("Track", StringComparison.OrdinalIgnoreCase)
            ? xmlTitle
            : CleanTitleFromFileName(fileName, activityType);

        var points = new List<TelemetryPointDto>();
        DateTime? firstTime = null;
        DateTime? lastTime = null;
        double totalDistanceKm = 0.0;
        Coordinates? prevCoord = null;
        var heartRates = new List<int>();

        for (int i = 0; i < trkpts.Count; i++)
        {
            var pt = trkpts[i];
            double lat = double.Parse(pt.Attribute("lat")?.Value ?? "0", CultureInfo.InvariantCulture);
            double lon = double.Parse(pt.Attribute("lon")?.Value ?? "0", CultureInfo.InvariantCulture);

            double? ele = null;
            var eleElem = pt.Elements().FirstOrDefault(e => e.Name.LocalName == "ele");
            if (eleElem != null && double.TryParse(eleElem.Value, CultureInfo.InvariantCulture, out double parsedEle))
            {
                ele = parsedEle;
            }

            DateTime timeUtc = DateTime.UtcNow;
            var timeElem = pt.Elements().FirstOrDefault(e => e.Name.LocalName == "time");
            if (timeElem != null && DateTime.TryParse(timeElem.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime parsedTime))
            {
                timeUtc = parsedTime;
            }

            firstTime ??= timeUtc;
            lastTime = timeUtc;

            int hr = 135;
            var hrElem = pt.Descendants().FirstOrDefault(e => e.Name.LocalName == "hr" || e.Name.LocalName == "HeartRateBpm");
            int? parsedHr = ExtractHeartRate(hrElem);
            if (parsedHr.HasValue)
            {
                hr = parsedHr.Value;
                heartRates.Add(hr);
            }

            var currentCoord = new Coordinates(lat, lon, ele);
            double instantSpeed = 0.0;
            if (prevCoord != null)
            {
                double segDist = prevCoord.DistanceTo(currentCoord).Kilometers;
                totalDistanceKm += segDist;
                if (points.Any())
                {
                    double segSec = (timeUtc - points[^1].TimestampUtc).TotalSeconds;
                    if (segSec > 0) instantSpeed = (segDist / segSec) * 3600.0;
                }
            }
            prevCoord = currentCoord;

            points.Add(new TelemetryPointDto
            {
                Latitude = lat,
                Longitude = lon,
                ElevationMeters = ele,
                TimestampUtc = timeUtc,
                InstantSpeedKmh = Math.Round(Math.Clamp(instantSpeed, 0, 100), 1),
                HeartRateBpm = hr
            });
        }

        DateTime start = firstTime ?? DateTime.UtcNow.AddMinutes(-30);
        DateTime end = lastTime ?? DateTime.UtcNow;
        if (end <= start) end = start.AddMinutes(Math.Max(5, points.Count * 2));

        int avgHr = heartRates.Any() ? (int)Math.Round(heartRates.Average()) : 138;
        int maxHr = heartRates.Any() ? heartRates.Max() : (int)Math.Round(avgHr * 1.15);

        int calories = CaloricExpenditure.Estimate(
            activityType,
            75.0,
            end - start,
            (end - start).TotalHours > 0 ? totalDistanceKm / (end - start).TotalHours : 0.0).Kcal;

        return new DeviceSyncTelemetryDto
        {
            DeviceSerialNumber = "GPX-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            DeviceModel = deviceName,
            Manufacturer = DetectManufacturer(deviceName),
            DeviceType = DeviceType.SmartBand,
            ActivityType = activityType,
            Title = finalTitle,
            StartTimeUtc = start,
            EndTimeUtc = end,
            ReportedDistanceKm = Math.Round(totalDistanceKm, 2),
            CaloriesBurned = calories,
            AvgHeartRateBpm = avgHr,
            MaxHeartRateBpm = maxHr,
            GpsPoints = points
        };
    }

    /// <summary>
    /// Faz o parsing completo de arquivos TCX (exportados por Mi Fitness, Samsung Health, Garmin Connect).
    /// Suporta tanto treinos outdoor com GPS quanto treinos indoor (Musculação, Levantamento de peso, etc.) sem Trackpoints de GPS.
    /// </summary>
    public DeviceSyncTelemetryDto ParseTcx(Stream tcxStream, string fileName = "", string deviceName = "Xiaomi / Samsung Health")
    {
        var doc = XDocument.Load(tcxStream);
        var actElem = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Activity");
        string sportAttr = actElem?.Attribute("Sport")?.Value?.ToLowerInvariant() ?? "";

        // Detectar Fabricante pelo atributo creator na tag raiz (ex: <TrainingCenterDatabase creator="Mi Fitness">)
        var creatorAttr = doc.Root?.Attribute("creator")?.Value ?? "";
        string manufacturer = DetectManufacturer(string.IsNullOrWhiteSpace(creatorAttr) ? deviceName : creatorAttr);
        string modelName = !string.IsNullOrWhiteSpace(creatorAttr) && creatorAttr.Contains("Mi Fitness", StringComparison.OrdinalIgnoreCase)
            ? "Xiaomi Mi Band 10 (Mi Fitness)"
            : deviceName;

        // Extrair Laps
        var laps = doc.Descendants().Where(e => e.Name.LocalName == "Lap").ToList();
        double lapTotalSeconds = 0.0;
        int lapCalories = 0;
        double lapDistanceMeters = 0.0;
        var lapHeartRates = new List<int>();
        var lapMaxHeartRates = new List<int>();

        foreach (var lap in laps)
        {
            var totalTimeElem = lap.Elements().FirstOrDefault(e => e.Name.LocalName == "TotalTimeSeconds");
            if (totalTimeElem != null && double.TryParse(totalTimeElem.Value, CultureInfo.InvariantCulture, out double sec))
            {
                lapTotalSeconds += sec;
            }

            var calElem = lap.Elements().FirstOrDefault(e => e.Name.LocalName == "Calories");
            if (calElem != null && int.TryParse(calElem.Value, out int cal))
            {
                lapCalories += cal;
            }

            var distElem = lap.Elements().FirstOrDefault(e => e.Name.LocalName == "DistanceMeters");
            if (distElem != null && double.TryParse(distElem.Value, CultureInfo.InvariantCulture, out double dist))
            {
                lapDistanceMeters += dist;
            }

            var hrElem = lap.Elements().FirstOrDefault(e => e.Name.LocalName == "HeartRateBpm" || e.Name.LocalName == "AverageHeartRateBpm");
            var hrVal = ExtractHeartRate(hrElem);
            if (hrVal.HasValue) lapHeartRates.Add(hrVal.Value);

            var maxHrElem = lap.Elements().FirstOrDefault(e => e.Name.LocalName == "MaximumHeartRateBpm");
            var maxHrVal = ExtractHeartRate(maxHrElem);
            if (maxHrVal.HasValue) lapMaxHeartRates.Add(maxHrVal.Value);
        }

        // Calorias no nível da Activity (Mi Fitness coloca <Calories>537</Calories> direto na Activity)
        int activityCalories = 0;
        var actCalElem = actElem?.Elements().FirstOrDefault(e => e.Name.LocalName == "Calories");
        if (actCalElem != null && int.TryParse(actCalElem.Value, out int actCal))
        {
            activityCalories = actCal;
        }
        int totalCalories = activityCalories > 0 ? activityCalories : lapCalories;

        // Horário de Início
        DateTime? startTime = null;
        var idElem = actElem?.Elements().FirstOrDefault(e => e.Name.LocalName == "Id");
        if (idElem != null && DateTime.TryParse(idElem.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime parsedIdTime))
        {
            startTime = parsedIdTime;
        }

        var firstLapStartElem = laps.FirstOrDefault()?.Attribute("StartTime");
        if (startTime == null && firstLapStartElem != null && DateTime.TryParse(firstLapStartElem.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime lapTime))
        {
            startTime = lapTime;
        }

        DateTime startUtc = startTime ?? DateTime.UtcNow.AddMinutes(-45);
        double durationSec = lapTotalSeconds > 0 ? lapTotalSeconds : 2400.0;
        DateTime endUtc = startUtc.AddSeconds(durationSec);

        // Processar Trackpoints se existirem
        var trackpoints = doc.Descendants().Where(e => e.Name.LocalName == "Trackpoint").ToList();
        var points = new List<TelemetryPointDto>();
        var pointHeartRates = new List<int>();
        double totalGpsDistKm = 0.0;
        Coordinates? prevCoord = null;

        foreach (var tp in trackpoints)
        {
            var timeElem = tp.Elements().FirstOrDefault(e => e.Name.LocalName == "Time");
            DateTime timeUtc = startUtc;
            if (timeElem != null && DateTime.TryParse(timeElem.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime ptTime))
            {
                timeUtc = ptTime;
            }

            var posElem = tp.Elements().FirstOrDefault(e => e.Name.LocalName == "Position");
            double? lat = null;
            double? lon = null;
            if (posElem != null)
            {
                var latElem = posElem.Elements().FirstOrDefault(e => e.Name.LocalName == "LatitudeDegrees");
                var lonElem = posElem.Elements().FirstOrDefault(e => e.Name.LocalName == "LongitudeDegrees");
                if (latElem != null && double.TryParse(latElem.Value, CultureInfo.InvariantCulture, out double pLat)) lat = pLat;
                if (lonElem != null && double.TryParse(lonElem.Value, CultureInfo.InvariantCulture, out double pLon)) lon = pLon;
            }

            double? ele = null;
            var altElem = tp.Elements().FirstOrDefault(e => e.Name.LocalName == "AltitudeMeters");
            if (altElem != null && double.TryParse(altElem.Value, CultureInfo.InvariantCulture, out double parsedAlt))
            {
                ele = parsedAlt;
            }

            var hrElem = tp.Elements().FirstOrDefault(e => e.Name.LocalName == "HeartRateBpm");
            int? tpBpm = ExtractHeartRate(hrElem);
            if (tpBpm.HasValue) pointHeartRates.Add(tpBpm.Value);

            if (lat.HasValue && lon.HasValue && (lat.Value != 0 || lon.Value != 0))
            {
                var coord = new Coordinates(lat.Value, lon.Value, ele);
                double speed = 0;
                if (prevCoord != null)
                {
                    double segKm = prevCoord.DistanceTo(coord).Kilometers;
                    totalGpsDistKm += segKm;
                    if (points.Any())
                    {
                        double segSec = (timeUtc - points[^1].TimestampUtc).TotalSeconds;
                        if (segSec > 0) speed = (segKm / segSec) * 3600.0;
                    }
                }
                prevCoord = coord;

                points.Add(new TelemetryPointDto
                {
                    Latitude = lat.Value,
                    Longitude = lon.Value,
                    ElevationMeters = ele,
                    TimestampUtc = timeUtc,
                    InstantSpeedKmh = Math.Round(Math.Clamp(speed, 0, 100), 1),
                    HeartRateBpm = tpBpm ?? (lapHeartRates.Any() ? lapHeartRates[0] : 135)
                });
            }
        }

        bool hasGpsCoordinates = points.Count >= 2;
        var activityType = DetermineActivityType(sportAttr, fileName, hasGpsCoordinates);
        string finalTitle = CleanTitleFromFileName(fileName, activityType);

        double reportedDistanceKm = totalGpsDistKm > 0
            ? totalGpsDistKm
            : (lapDistanceMeters > 0 ? lapDistanceMeters / 1000.0 : 0.0);

        // Se for Musculação/Levantamento de peso, distância real é estática (0.0 km)
        if (activityType == ActivityType.StrengthTraining)
        {
            reportedDistanceKm = 0.0;
        }

        // Frequência Cardíaca
        int avgHr;
        int maxHr;

        if (pointHeartRates.Any())
        {
            avgHr = (int)Math.Round(pointHeartRates.Average());
            maxHr = pointHeartRates.Max();
        }
        else if (lapHeartRates.Any())
        {
            avgHr = (int)Math.Round(lapHeartRates.Average());
            maxHr = lapMaxHeartRates.Any() ? lapMaxHeartRates.Max() : (int)Math.Round(avgHr * 1.15);
        }
        else
        {
            avgHr = 135;
            maxHr = 155;
        }

        // Se o arquivo não continha calorias, calcula estimativa biológica
        if (totalCalories <= 0)
        {
            totalCalories = CaloricExpenditure.Estimate(
                activityType,
                75.0,
                endUtc - startUtc,
                (endUtc - startUtc).TotalHours > 0 ? reportedDistanceKm / (endUtc - startUtc).TotalHours : 0.0).Kcal;
        }

        return new DeviceSyncTelemetryDto
        {
            DeviceSerialNumber = "TCX-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            DeviceModel = modelName,
            Manufacturer = manufacturer,
            DeviceType = DeviceType.SmartBand,
            ActivityType = activityType,
            Title = finalTitle,
            StartTimeUtc = startUtc,
            EndTimeUtc = endUtc,
            ReportedDistanceKm = Math.Round(reportedDistanceKm, 2),
            CaloriesBurned = totalCalories,
            AvgHeartRateBpm = avgHr,
            MaxHeartRateBpm = maxHr,
            GpsPoints = points
        };
    }

    /// <summary>
    /// Extrai o valor de frequência cardíaca suportando os formatos Mi Fitness e padrão Garmin.
    /// </summary>
    private static int? ExtractHeartRate(XElement? element)
    {
        if (element == null) return null;

        // Padrão Garmin: <HeartRateBpm><Value>135</Value></HeartRateBpm>
        var valElem = element.Elements().FirstOrDefault(e => e.Name.LocalName == "Value");
        if (valElem != null && int.TryParse(valElem.Value, out int v1) && v1 > 30 && v1 < 250)
        {
            return v1;
        }

        // Padrão Xiaomi Mi Fitness: <HeartRateBpm>135</HeartRateBpm>
        if (int.TryParse(element.Value, out int v2) && v2 > 30 && v2 < 250)
        {
            return v2;
        }

        return null;
    }

    /// <summary>
    /// Determina o tipo de atividade física com base no atributo Sport do XML, nome do arquivo e presença de GPS.
    /// </summary>
    private static ActivityType DetermineActivityType(string sportAttr, string fileName, bool hasGpsCoordinates)
    {
        string combined = $"{sportAttr} {fileName}".ToLowerInvariant();

        if (combined.Contains("levantamento de peso") ||
            combined.Contains("musculação") ||
            combined.Contains("musculacao") ||
            combined.Contains("strength") ||
            combined.Contains("weight") ||
            combined.Contains("força") ||
            combined.Contains("forca") ||
            combined.Contains("fitness") ||
            combined.Contains("gym") ||
            combined.Contains("academia") ||
            combined.Contains("crossfit") ||
            combined.Contains("bodybuilding") ||
            combined.Contains("funcional"))
        {
            return ActivityType.StrengthTraining;
        }

        if (combined.Contains("corrida") ||
            combined.Contains("run") ||
            combined.Contains("running") ||
            combined.Contains("jogging") ||
            combined.Contains("esteira") ||
            combined.Contains("treadmill"))
        {
            return ActivityType.Running;
        }

        if (combined.Contains("ciclismo") ||
            combined.Contains("cycling") ||
            combined.Contains("bike") ||
            combined.Contains("pedal") ||
            combined.Contains("spinning") ||
            combined.Contains("bicicleta"))
        {
            return ActivityType.Cycling;
        }

        if (combined.Contains("caminhada") ||
            combined.Contains("walking") ||
            combined.Contains("walk") ||
            combined.Contains("trilha") ||
            combined.Contains("hike") ||
            combined.Contains("hiking"))
        {
            return ActivityType.Walking;
        }

        // Se não possui percurso GPS registrado, classifica como treino indoor (Musculação / Força)
        if (!hasGpsCoordinates)
        {
            return ActivityType.StrengthTraining;
        }

        return ActivityType.Running;
    }

    /// <summary>
    /// Limpa o nome do arquivo para exibir um título elegante na interface.
    /// Exemplo: "20261017Levantamento de peso.tcx" -> "Levantamento de Peso".
    /// </summary>
    private static string CleanTitleFromFileName(string fileName, ActivityType activityType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return activityType switch
            {
                ActivityType.StrengthTraining => "Treino de Musculação",
                ActivityType.Running => "Corrida",
                ActivityType.Cycling => "Ciclismo",
                ActivityType.Walking => "Caminhada",
                _ => "Atividade Física"
            };
        }

        string name = Path.GetFileNameWithoutExtension(fileName);
        // Remove prefixos numéricos de data como 20261017 ou 2026-10-17
        name = Regex.Replace(name, @"^\d{4}[-_]?\d{2}[-_]?\d{2}[-_]?", "");
        name = name.Replace('_', ' ').Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return activityType == ActivityType.StrengthTraining ? "Treino de Musculação" : "Treino Real";
        }

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(name.ToLowerInvariant());
    }

    private static string DetectManufacturer(string hint)
    {
        if (string.IsNullOrWhiteSpace(hint)) return "Xiaomi";
        if (hint.Contains("samsung", StringComparison.OrdinalIgnoreCase)) return "Samsung";
        if (hint.Contains("garmin", StringComparison.OrdinalIgnoreCase)) return "Garmin";
        if (hint.Contains("polar", StringComparison.OrdinalIgnoreCase)) return "Polar";
        return "Xiaomi";
    }
}
