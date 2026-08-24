using System.Text.Json.Serialization;

namespace AtcSimulationFramework.Api.DTOs;

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
