using KmValidator.Application.DTOs;
using KmValidator.Application.Ports;
using KmValidator.Domain.Enums;

namespace KmValidator.Application.UseCases;

public class SimulateSmartwatchSyncUseCase : ISimulateSmartwatchSyncUseCase
{
    private readonly IIngestActivityUseCase _ingestUseCase;

    public SimulateSmartwatchSyncUseCase(IIngestActivityUseCase ingestUseCase)
    {
        _ingestUseCase = ingestUseCase;
    }

    public async Task<ActivitySummaryDto> ExecuteAsync(SimulationRequestDto request, CancellationToken cancellationToken = default)
    {
        var scenario = request.Scenario?.ToLowerInvariant() ?? "valid_5k_run";
        var deviceModel = string.IsNullOrWhiteSpace(request.DeviceModel) ? "Garmin Forerunner 965" : request.DeviceModel;

        var telemetry = scenario switch
        {
            "miband_strength_training" => BuildMiBandStrengthTrainingScenario(deviceModel),
            "miband_run" => BuildMiBandRunScenario(deviceModel),
            "valid_cycling" => BuildValidCyclingScenario(deviceModel),
            "fraud_vehicle_run" => BuildFraudVehicleScenario(deviceModel),
            "gps_glitch" => BuildGpsGlitchScenario(deviceModel),
            "abnormal_resting_hr" => BuildAbnormalRestingHrScenario(deviceModel),
            _ => BuildValid5kRunScenario(deviceModel)
        };

        return await _ingestUseCase.ExecuteAsync(telemetry, cancellationToken);
    }

    private static DeviceSyncTelemetryDto BuildValid5kRunScenario(string model)
    {
        var now = DateTime.UtcNow;
        var start = now.AddMinutes(-28);
        var points = new List<TelemetryPointDto>();

        // Circuito no Parque Ibirapuera / São Paulo (-23.5874, -46.6576)
        double baseLat = -23.5874;
        double baseLon = -46.6576;
        int numPoints = 30;
        double radius = 0.008; // ~800m de raio para fechar ~5.0 km em voltas

        for (int i = 0; i <= numPoints; i++)
        {
            double angle = (2.0 * Math.PI / numPoints) * i;
            double lat = baseLat + radius * Math.Sin(angle);
            double lon = baseLon + radius * Math.Cos(angle);
            var timestamp = start.AddSeconds(i * 56);
            double speed = 10.5 + Math.Sin(i) * 1.2; // ~10.5 km/h
            int hr = 145 + (int)(Math.Sin(i) * 12) + (i / 3);

            points.Add(new TelemetryPointDto
            {
                Latitude = Math.Round(lat, 6),
                Longitude = Math.Round(lon, 6),
                ElevationMeters = 760.0 + Math.Sin(i * 0.5) * 15.0,
                TimestampUtc = timestamp,
                InstantSpeedKmh = Math.Round(speed, 1),
                HeartRateBpm = Math.Clamp(hr, 135, 175)
            });
        }

        return new DeviceSyncTelemetryDto
        {
            DeviceSerialNumber = "GARMIN-965-BR-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            DeviceModel = model,
            Manufacturer = "Garmin",
            DeviceType = DeviceType.Smartwatch,
            ActivityType = ActivityType.Running,
            Title = "Corrida Matinal - Parque Ibirapuera",
            StartTimeUtc = start,
            EndTimeUtc = start.AddMinutes(28),
            ReportedDistanceKm = 5.05,
            CaloriesBurned = 345,
            AvgHeartRateBpm = 156,
            MaxHeartRateBpm = 174,
            GpsPoints = points
        };
    }

    private static DeviceSyncTelemetryDto BuildValidCyclingScenario(string model)
    {
        var now = DateTime.UtcNow;
        var start = now.AddMinutes(-50);
        var points = new List<TelemetryPointDto>();

        double baseLat = -23.5505;
        double baseLon = -46.6333;
        int numPoints = 40;

        for (int i = 0; i <= numPoints; i++)
        {
            double lat = baseLat + (i * 0.004);
            double lon = baseLon + (Math.Sin(i * 0.4) * 0.006);
            var timestamp = start.AddSeconds(i * 75);
            double speed = 25.0 + Math.Sin(i) * 4.0;
            int hr = 138 + (int)(Math.Sin(i) * 10);

            points.Add(new TelemetryPointDto
            {
                Latitude = Math.Round(lat, 6),
                Longitude = Math.Round(lon, 6),
                ElevationMeters = 720.0 + i * 2,
                TimestampUtc = timestamp,
                InstantSpeedKmh = Math.Round(speed, 1),
                HeartRateBpm = Math.Clamp(hr, 125, 160)
            });
        }

        return new DeviceSyncTelemetryDto
        {
            DeviceSerialNumber = "POLAR-V3-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            DeviceModel = string.IsNullOrWhiteSpace(model) ? "Polar Vantage V3" : model,
            Manufacturer = "Polar",
            DeviceType = DeviceType.Smartwatch,
            ActivityType = ActivityType.Cycling,
            Title = "Treino de Ciclismo - Ciclovia Marginal",
            StartTimeUtc = start,
            EndTimeUtc = start.AddMinutes(50),
            ReportedDistanceKm = 21.8,
            CaloriesBurned = 510,
            AvgHeartRateBpm = 142,
            MaxHeartRateBpm = 162,
            GpsPoints = points
        };
    }

    private static DeviceSyncTelemetryDto BuildFraudVehicleScenario(string model)
    {
        var now = DateTime.UtcNow;
        var start = now.AddMinutes(-15);
        var points = new List<TelemetryPointDto>();

        double baseLat = -23.5600;
        double baseLon = -46.6800;
        int numPoints = 20;

        for (int i = 0; i <= numPoints; i++)
        {
            // Trajeto em linha reta com alta velocidade pela avenida
            double lat = baseLat + (i * 0.006);
            double lon = baseLon + (i * 0.005);
            var timestamp = start.AddSeconds(i * 45);
            double speed = 55.0 + Math.Sin(i) * 15.0; // 55 a 70 km/h alegando ser corrida!
            int hr = 62; // Batimento cardíaco de quem está sentado no banco do carro

            points.Add(new TelemetryPointDto
            {
                Latitude = Math.Round(lat, 6),
                Longitude = Math.Round(lon, 6),
                ElevationMeters = 750.0,
                TimestampUtc = timestamp,
                InstantSpeedKmh = Math.Round(speed, 1),
                HeartRateBpm = hr
            });
        }

        return new DeviceSyncTelemetryDto
        {
            DeviceSerialNumber = "FRAUD-WATCH-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            DeviceModel = "Apple Watch Ultra 2",
            Manufacturer = "Apple",
            DeviceType = DeviceType.Smartwatch,
            ActivityType = ActivityType.Running, // Usuário marcou como Corrida!
            Title = "Deslocamento de Carro Disfarçado de Corrida",
            StartTimeUtc = start,
            EndTimeUtc = start.AddMinutes(15),
            ReportedDistanceKm = 14.5,
            CaloriesBurned = 150,
            AvgHeartRateBpm = 64, // Frequência de repouso!
            MaxHeartRateBpm = 68,
            GpsPoints = points
        };
    }

    private static DeviceSyncTelemetryDto BuildGpsGlitchScenario(string model)
    {
        var now = DateTime.UtcNow;
        var start = now.AddMinutes(-30);
        var points = new List<TelemetryPointDto>();

        double baseLat = -23.5800;
        double baseLon = -46.6500;
        int numPoints = 25;

        for (int i = 0; i <= numPoints; i++)
        {
            double lat = baseLat + (i * 0.001);
            double lon = baseLon + (i * 0.001);

            // Inserir salto abrupto de GPS no meio (teleporte por erro de satélite)
            if (i == 12)
            {
                lat += 0.04; // Salto instantâneo de ~4 km
                lon += 0.03;
            }

            var timestamp = start.AddSeconds(i * 70);

            points.Add(new TelemetryPointDto
            {
                Latitude = Math.Round(lat, 6),
                Longitude = Math.Round(lon, 6),
                ElevationMeters = 750.0,
                TimestampUtc = timestamp,
                InstantSpeedKmh = i == 12 ? 180.0 : 9.5,
                HeartRateBpm = 148
            });
        }

        return new DeviceSyncTelemetryDto
        {
            DeviceSerialNumber = "GLITCH-WATCH-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            DeviceModel = "Samsung Galaxy Watch 6",
            Manufacturer = "Samsung",
            DeviceType = DeviceType.Smartwatch,
            ActivityType = ActivityType.Running,
            Title = "Corrida com Salto de GPS e Divergência",
            StartTimeUtc = start,
            EndTimeUtc = start.AddMinutes(30),
            ReportedDistanceKm = 11.2, // Relógio computou salto errôneo
            CaloriesBurned = 320,
            AvgHeartRateBpm = 148,
            MaxHeartRateBpm = 160,
            GpsPoints = points
        };
    }

    private static DeviceSyncTelemetryDto BuildAbnormalRestingHrScenario(string model)
    {
        var now = DateTime.UtcNow;
        var start = now.AddMinutes(-20);
        var points = new List<TelemetryPointDto>();

        double baseLat = -23.5900;
        double baseLon = -46.6700;
        int numPoints = 20;

        for (int i = 0; i <= numPoints; i++)
        {
            double lat = baseLat + (i * 0.0015);
            double lon = baseLon + (i * 0.0012);
            var timestamp = start.AddSeconds(i * 60);

            points.Add(new TelemetryPointDto
            {
                Latitude = Math.Round(lat, 6),
                Longitude = Math.Round(lon, 6),
                ElevationMeters = 740.0,
                TimestampUtc = timestamp,
                InstantSpeedKmh = 12.0,
                HeartRateBpm = 42 // Fisiologicamente impossível em esforço de 12 km/h
            });
        }

        return new DeviceSyncTelemetryDto
        {
            DeviceSerialNumber = "NO-SENSOR-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            DeviceModel = "Garmin Instinct 2",
            Manufacturer = "Garmin",
            DeviceType = DeviceType.Smartwatch,
            ActivityType = ActivityType.Running,
            Title = "Corrida 4 km com Relógio Sem Contato na Pele",
            StartTimeUtc = start,
            EndTimeUtc = start.AddMinutes(20),
            ReportedDistanceKm = 4.0,
            CaloriesBurned = 260,
            AvgHeartRateBpm = 42, // Anomalia biométrica
            MaxHeartRateBpm = 45,
            GpsPoints = points
        };
    }

    private static DeviceSyncTelemetryDto BuildMiBandStrengthTrainingScenario(string model)
    {
        var now = DateTime.UtcNow;
        var start = now.AddMinutes(-48);
        var points = new List<TelemetryPointDto>();

        // Coordenada fixa da Academia / Centro de Treinamento em SP (-23.5615, -46.6559)
        double gymLat = -23.5615;
        double gymLon = -46.6559;

        // Variação de BPM simulando séries de musculação (supino, agachamento, terra) e descanso
        int[] setHrValues = { 108, 142, 165, 122, 158, 169, 118, 155, 164, 115, 148, 160 };

        for (int i = 0; i < setHrValues.Length; i++)
        {
            points.Add(new TelemetryPointDto
            {
                Latitude = gymLat + (i * 0.00001), // micro-movimentação dentro da sala de musculação
                Longitude = gymLon + (i * 0.00001),
                ElevationMeters = 760.0,
                TimestampUtc = start.AddMinutes(i * 4),
                InstantSpeedKmh = 0.0, // parado fazendo musculação
                HeartRateBpm = setHrValues[i]
            });
        }

        return new DeviceSyncTelemetryDto
        {
            DeviceSerialNumber = "MIBAND10-BLE-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            DeviceModel = string.IsNullOrWhiteSpace(model) ? "Xiaomi Mi Band 10" : model,
            Manufacturer = "Xiaomi",
            DeviceType = DeviceType.SmartBand,
            ActivityType = ActivityType.StrengthTraining,
            Title = "Treino de Musculação (Hipertrofia)",
            StartTimeUtc = start,
            EndTimeUtc = start.AddMinutes(48),
            ReportedDistanceKm = 0.0, // Musculação indoor
            CaloriesBurned = 425,
            AvgHeartRateBpm = 138,
            MaxHeartRateBpm = 169,
            GpsPoints = points
        };
    }

    private static DeviceSyncTelemetryDto BuildMiBandRunScenario(string model)
    {
        var now = DateTime.UtcNow;
        var start = now.AddMinutes(-32);
        var points = new List<TelemetryPointDto>();

        double baseLat = -23.5700;
        double baseLon = -46.6400;

        for (int i = 0; i <= 25; i++)
        {
            double lat = baseLat + (i * 0.002);
            double lon = baseLon + (Math.Sin(i * 0.3) * 0.002);
            var timestamp = start.AddSeconds(i * 76);
            double speed = 11.2 + Math.Sin(i) * 1.5;
            int hr = 148 + (int)(Math.Sin(i) * 14);

            points.Add(new TelemetryPointDto
            {
                Latitude = Math.Round(lat, 6),
                Longitude = Math.Round(lon, 6),
                ElevationMeters = 750.0,
                TimestampUtc = timestamp,
                InstantSpeedKmh = Math.Round(speed, 1),
                HeartRateBpm = Math.Clamp(hr, 140, 172)
            });
        }

        return new DeviceSyncTelemetryDto
        {
            DeviceSerialNumber = "MIBAND10-BLE-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            DeviceModel = string.IsNullOrWhiteSpace(model) ? "Xiaomi Mi Band 10" : model,
            Manufacturer = "Xiaomi",
            DeviceType = DeviceType.SmartBand,
            ActivityType = ActivityType.Running,
            Title = "Corrida Urbana Conectada via Bluetooth",
            StartTimeUtc = start,
            EndTimeUtc = start.AddMinutes(32),
            ReportedDistanceKm = 6.1,
            CaloriesBurned = 390,
            AvgHeartRateBpm = 154,
            MaxHeartRateBpm = 172,
            GpsPoints = points
        };
    }
}
