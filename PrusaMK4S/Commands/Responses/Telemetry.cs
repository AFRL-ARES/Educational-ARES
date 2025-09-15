using System.Text.Json.Serialization;

namespace PrusaMK4S.Commands.Responses;

public class Telemetry
{
  [JsonPropertyName("temp-bed")]
  public double TempBed { get; set; }

  [JsonPropertyName("temp-nozzle")]
  public double TempNozzle { get; set; }

  [JsonPropertyName("print-speed")]
  public int PrintSpeed { get; set; }

  [JsonPropertyName("z-height")]
  public double ZHeight { get; set; }

  [JsonPropertyName("material")]
  public string Material { get; set; } = string.Empty;
}
