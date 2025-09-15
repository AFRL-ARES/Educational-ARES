using System.Text.Json.Serialization;

namespace PrusaMK4S.Commands.Responses;

public class Tool
{
  [JsonPropertyName("actual")]
  public double Actual { get; set; }

  [JsonPropertyName("target")]
  public double Target { get; set; }

  [JsonPropertyName("display")]
  public double Display { get; set; }

  [JsonPropertyName("offset")]
  public int Offset { get; set; }

}
