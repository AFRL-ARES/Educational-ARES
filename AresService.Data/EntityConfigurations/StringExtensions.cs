using Google.Protobuf.Collections;

namespace AresService.Data.EntityConfigurations;
internal static class StringExtensions
{
  public static RepeatedField<string> ToRepeatedField(this string value, char separator)
  {
    var split = value.Split(separator);
    var rf = new RepeatedField<string>();
    rf.AddRange(split);
    return rf;
  }
}
