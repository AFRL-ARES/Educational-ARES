using System.Text.Json.Serialization;

namespace PrusaMK4S.Commands.Responses;

public class Temperature
{
  [JsonPropertyName("tool0")]
  public Tool Tool { get; set; } = new Tool();

  [JsonPropertyName("bed")]
  public Tool Bed { get; set; } = new Tool();
}
