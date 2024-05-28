using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EventInfo.Models;

public class EnvironmentalSensorTelemetryDTO
{
    [JsonPropertyName("_time")]
    public DateTime Time { get; set; }
    [JsonPropertyName("carbon_oxide")]
    public double CarbonOxide { get; set; }
    public string? Device { get; set; }
    public double Humidity { get; set; }
    public bool Light { get; set; }
    [JsonPropertyName("liquid_petroleum_gas")]
    public double LiquidPetroleumGas { get; set; }
    public bool Motion { get; set; }
    public double Smoke { get; set; }
    public double Temperature { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
}