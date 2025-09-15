using System.Text.Json.Serialization;

namespace PrusaMK4S.Commands.Responses;

public class Flags
{
  [JsonPropertyName("operational")]
  public bool Operational { get; set; }

  [JsonPropertyName("paused")]
  public bool Paused { get; set; }

  [JsonPropertyName("printing")]
  public bool Printing { get; set; }

  [JsonPropertyName("cancelling")]
  public bool Cancelling { get; set; }

  [JsonPropertyName("pausing")]
  public bool Pausing { get; set; }

  [JsonPropertyName("error")]
  public bool Error { get; set; }

  [JsonPropertyName("sdReady")]
  public bool SdReady { get; set; }

  [JsonPropertyName("closedOnError")]
  public bool ClosedOnError { get; set; }

  [JsonPropertyName("ready")]
  public bool Ready { get; set; }

  [JsonPropertyName("busy")]
  public bool Busy { get; set; }
}
