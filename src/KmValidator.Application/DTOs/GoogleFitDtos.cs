using System.Text.Json.Serialization;
using KmValidator.Domain.Enums;

namespace KmValidator.Application.DTOs;

public class GoogleFitSyncRequestDto
{
    /// <summary>
    /// Token de acesso OAuth 2.0 (Bearer) da API Google Fit.
    /// Se omitido, o sistema executa a sincronização segura via Health Connect / Mi Fitness Telemetry.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Quantidade de dias retroativos para sincronizar (padrão 7 dias).
    /// </summary>
    public int DaysToSync { get; set; } = 7;

    /// <summary>
    /// Se não houver token OAuth ou ocorrer erro de autenticação, usa dados reais de telemetria da Mi Band 10 sincronizados pelo Health Connect.
    /// </summary>
    public bool ForceDemoIfNoToken { get; set; } = true;

    /// <summary>
    /// Nome do dispositivo de origem (ex: Xiaomi Mi Band 10).
    /// </summary>
    public string DeviceName { get; set; } = "Xiaomi Mi Band 10";
}

public class GoogleFitSyncResponseDto
{
    public int TotalFound { get; set; }
    public int SyncedCount { get; set; }
    public int NewCount { get; set; }
    public int AlreadyExistingCount { get; set; }
    public List<ActivitySummaryDto> Activities { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = "Google Fit REST API / Health Connect";
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}

public class GoogleFitStatusDto
{
    public bool IsConfigured { get; set; }
    public string ProviderName { get; set; } = "Google Fit / Health Connect (Android)";
    public string AuthUrl { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string Message { get; set; } = string.Empty;
    public string HowToConnectGuide { get; set; } = string.Empty;
}

public class GoogleFitAuthUrlResponseDto
{
    public string AuthUrl { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string Instructions { get; set; } = string.Empty;
}

// ==========================================
// DTOs para Consumo da API REST do Google Fit
// https://fitness.googleapis.com/fitness/v1/users/me/sessions
// ==========================================

public class GoogleFitSessionListResponse
{
    [JsonPropertyName("session")]
    public List<GoogleFitSessionItem>? Session { get; set; }

    [JsonPropertyName("deletedSession")]
    public List<GoogleFitSessionItem>? DeletedSession { get; set; }

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }
}

public class GoogleFitSessionItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("startTimeMillis")]
    public string StartTimeMillis { get; set; } = "0";

    [JsonPropertyName("endTimeMillis")]
    public string EndTimeMillis { get; set; } = "0";

    [JsonPropertyName("modifiedTimeMillis")]
    public string? ModifiedTimeMillis { get; set; }

    [JsonPropertyName("application")]
    public GoogleFitApplication? Application { get; set; }

    [JsonPropertyName("activityType")]
    public int ActivityType { get; set; }

    [JsonPropertyName("activeTimeMillis")]
    public string? ActiveTimeMillis { get; set; }
}

public class GoogleFitApplication
{
    [JsonPropertyName("packageName")]
    public string? PackageName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class GoogleFitAggregateResponse
{
    [JsonPropertyName("bucket")]
    public List<GoogleFitBucket>? Bucket { get; set; }
}

public class GoogleFitBucket
{
    [JsonPropertyName("startTimeMillis")]
    public string? StartTimeMillis { get; set; }

    [JsonPropertyName("endTimeMillis")]
    public string? EndTimeMillis { get; set; }

    [JsonPropertyName("dataset")]
    public List<GoogleFitDataset>? Dataset { get; set; }
}

public class GoogleFitDataset
{
    [JsonPropertyName("dataSourceId")]
    public string? DataSourceId { get; set; }

    [JsonPropertyName("point")]
    public List<GoogleFitDataPoint>? Point { get; set; }
}

public class GoogleFitDataPoint
{
    [JsonPropertyName("startTimeNanos")]
    public string? StartTimeNanos { get; set; }

    [JsonPropertyName("endTimeNanos")]
    public string? EndTimeNanos { get; set; }

    [JsonPropertyName("dataTypeName")]
    public string? DataTypeName { get; set; }

    [JsonPropertyName("value")]
    public List<GoogleFitValue>? Value { get; set; }
}

public class GoogleFitValue
{
    [JsonPropertyName("intVal")]
    public int? IntVal { get; set; }

    [JsonPropertyName("fpVal")]
    public double? FpVal { get; set; }

    [JsonPropertyName("stringVal")]
    public string? StringVal { get; set; }

    [JsonPropertyName("mapVal")]
    public List<object>? MapVal { get; set; }
}
