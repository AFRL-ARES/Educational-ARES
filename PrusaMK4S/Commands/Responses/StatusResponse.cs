using System.Text.Json.Serialization;

namespace PrusaMK4S.Commands.Responses;

public class StatusResponse
{
  [JsonPropertyName("telemetry")]
  public Telemetry? Telemetry { get; set; }

  [JsonPropertyName("temperature")]
  public Temperature Temperature { get; set; } = new Temperature();

  [JsonPropertyName("state")]
  public State State { get; set; } = new State();

}
