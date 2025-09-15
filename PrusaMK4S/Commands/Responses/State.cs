using System.Text.Json.Serialization;

namespace PrusaMK4S.Commands.Responses;

public class State
{
  [JsonPropertyName("text")]
  public string Text { get; set; } = string.Empty;

  [JsonPropertyName("flags")]
  public Flags Flags { get; set; }
}
