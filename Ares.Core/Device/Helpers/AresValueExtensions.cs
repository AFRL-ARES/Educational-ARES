using Ares.Datamodel;

namespace Ares.Core.Device.Helpers;
internal static class AresValueExtensions
{
  public static string GetValueAsString(this AresValue value)
  {
    return value.KindCase switch
    {
      AresValue.KindOneofCase.NullValue => "null",
      AresValue.KindOneofCase.BoolValue => value.BoolValue.ToString(),
      AresValue.KindOneofCase.StringValue => value.StringValue,
      AresValue.KindOneofCase.NumberValue => value.NumberValue.ToString(),
      AresValue.KindOneofCase.StringArrayValue => "[" + string.Join(", ", value.StringArrayValue.Strings) + "]",
      AresValue.KindOneofCase.NumberArrayValue => "[" + string.Join(", ", value.NumberArrayValue.Numbers) + "]",
      AresValue.KindOneofCase.BytesValue => Convert.ToBase64String(value.BytesValue.ToByteArray()),
      AresValue.KindOneofCase.BoolArrayValue => "[" + string.Join(", ", value.BoolArrayValue.Bools) + "]",
      _ => string.Empty,
    };
  }
}
