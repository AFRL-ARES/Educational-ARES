using System;
using System.Linq;
using System.Text;

namespace Ares.Device.Serial.Commands;

/// <summary>
/// This is a special parser that treats byte data as readable ASCII characters in the range of
/// 32 to 127 and filters out any bytes that fall outside of that range. We also include new line
/// characters and cariage returns
/// So if you expect to only get human readable outputs while filtering out the garbage,
/// this class should be extended instead of the base SerialResponseParser
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public abstract class AsciiResponseParser<TResponse> : SerialResponseParser<TResponse> where TResponse : SerialResponse
{
  private readonly string _lineSeparator;

  protected AsciiResponseParser(string lineSeparator = "\r")
  {
    _lineSeparator = lineSeparator;
  }

  private bool TryParseResponse(string[] bufferLines, out TResponse? response, out int parsedLineIndex)
  {
    for(var i = 0; i < bufferLines.Length; i++)
    {
      if(bufferLines[i].StartsWith(_lineSeparator))
      {

      }
      if(TryParseResponse(bufferLines[i], out response))
      {
        parsedLineIndex = i;
        return true;
      }
    }

    parsedLineIndex = -1;
    response = null;
    return false;
  }

  protected abstract bool TryParseResponse(string line, out TResponse? response);

  public override bool TryParseResponse(byte[] buffer, out TResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    var asciiBufferProxy = Encoding.UTF8.GetString(buffer);
    var useLast = asciiBufferProxy.EndsWith(_lineSeparator);
    var availableLines = asciiBufferProxy.Split(_lineSeparator, StringSplitOptions.RemoveEmptyEntries)[..^(useLast ? 0 : 1)];
    availableLines = availableLines.Select(line => line = string.Concat(
      line.Where(ch => (ch >= 32 && ch < 127) || ch == 10 || ch == 13))).ToArray();

    try
    {
      if(!TryParseResponse(availableLines, out response, out var parsedLineIndex))
      {
        response = null;
        dataToRemove = null;
        return false;
      }

      var skippedBytes = availableLines[..parsedLineIndex].Sum(s => s.Length + 1);// add 1 for the line separator in the buffer

      var processedSize = availableLines[parsedLineIndex].Length + 1;// add 1 for the line separator in the buffer

      dataToRemove = new ArraySegment<byte>(buffer, skippedBytes, processedSize);
      return true;
    }
    catch
    {
      response = null;
      dataToRemove = null;
      return false;
    }
  }
}
