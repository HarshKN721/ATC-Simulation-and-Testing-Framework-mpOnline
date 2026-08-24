using System.Text.Json.Serialization;

namespace AtcSimulationFramework.Api.DTOs;

public class StartSimulationRunResponse
{
    [JsonPropertyName("simulationRunId")]
    public int SimulationRunId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }
}

public class StopSimulationRunResponse
{
    [JsonPropertyName("simulationRunId")]
    public int SimulationRunId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }
}

public class AircraftPositionDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("altitude")]
    public double Altitude { get; set; }

    [JsonPropertyName("heading")]
    public double Heading { get; set; }

    [JsonPropertyName("speed")]
    public double Speed { get; set; }
}

public class AircraftPositionsResponse
{
    [JsonPropertyName("simulationRunId")]
    public int SimulationRunId { get; set; }

    [JsonPropertyName("aircraft")]
    public IEnumerable<AircraftPositionDto> Aircraft { get; set; } = new List<AircraftPositionDto>();
}

public class ConflictEventDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("simulationRunId")]
    public int SimulationRunId { get; set; }

    [JsonPropertyName("aircraftAId")]
    public int AircraftAId { get; set; }

    [JsonPropertyName("aircraftAName")]
    public string AircraftAName { get; set; } = string.Empty;

    [JsonPropertyName("aircraftBId")]
    public int AircraftBId { get; set; }

    [JsonPropertyName("aircraftBName")]
    public string AircraftBName { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("isResolved")]
    public bool IsResolved { get; set; }

    [JsonPropertyName("resolutionTimestamp")]
    public DateTime? ResolutionTimestamp { get; set; }
}

public class ConflictEventsResponse
{
    [JsonPropertyName("simulationRunId")]
    public int SimulationRunId { get; set; }

    [JsonPropertyName("conflicts")]
    public IEnumerable<ConflictEventDto> Conflicts { get; set; } = new List<ConflictEventDto>();
}
