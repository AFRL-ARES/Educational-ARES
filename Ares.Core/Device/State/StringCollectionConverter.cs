using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Ares.Core.Device.State;

public class StringCollectionConverter : ITypeConverter
{
  public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
  {
    if(text is null)
      return Array.Empty<string>();

    return new List<string>(text.Split(','));
  }

  public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
  {
    if(value is not IEnumerable<string> strings)
      return "";

    return string.Join(',', strings);
  }
}
