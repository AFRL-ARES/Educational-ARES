namespace Ares.Core.Device.State.Export.ExportStreamProviders;

public record ExportStateStream(Stream Stream)
{
  public ExportStateStream(Stream stream, string fileExtension) : this(stream)
  {
    FileExtension = fileExtension;
  }
  /// <summary>
  /// If this stream can be exported to a file, file extension can be defined here
  /// </summary>
  public string FileExtension { get; set; } = string.Empty;
}
