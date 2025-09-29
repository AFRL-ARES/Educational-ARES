
using Ares.Services;

namespace UI.Backend.Extensions;

public static class ExportTypeExtensions
{
  public static string ToFileExtension(this ExportType type) => type switch
  {
    ExportType.Unspecified => "",
    ExportType.Zipped => "zip",
    ExportType.Combined => "csv",
    _ => ""
  };
}
