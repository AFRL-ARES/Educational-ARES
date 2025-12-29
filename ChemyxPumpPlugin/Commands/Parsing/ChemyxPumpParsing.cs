using System.Text;
using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Parsing;

internal static class ChemyxPumpParsing
{
  public static bool TryParse(byte[] buffer, string originalCommand, out ChemyxPumpResponse response, out ArraySegment<byte>? dataToRemove)
  {
    response = default!;
    dataToRemove = null;

    if(buffer is null || buffer.Length == 0)
      return false;

    var terminatorIdx = Array.IndexOf(buffer, (byte)'>');
    if(terminatorIdx < 0)
      return false;

    var utfProxy = Encoding.UTF8.GetString(buffer);
    var useLast = utfProxy.EndsWith('>');
    var commandResponses = utfProxy.Split('>', StringSplitOptions.None);
    if(!useLast)
    {
      commandResponses = commandResponses.SkipLast(1).ToArray();
    }
    var startIdx = 0;

    for(var i = 0; i < commandResponses.Length; i++)
    {
      var cmdResponse = commandResponses[i];
      var lines = cmdResponse.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries);
      var commandEcho = lines.FirstOrDefault() ?? string.Empty;
      if(commandEcho != originalCommand)
      {
        startIdx += cmdResponse.Length + 1; // account for the response payload plus its '>' terminator
        continue;
      }
      var payload = lines.Skip(1).ToArray();
      response = new ChemyxPumpResponse(commandEcho, payload, cmdResponse);
      dataToRemove = new ArraySegment<byte>(buffer, startIdx, cmdResponse.Length + 1);
      return true;
    }

    return false;
  }

  public static string ToPrintable(string text)
  {
    var sb = new StringBuilder();
    foreach(char c in text)
    {
      // Use Unicode categories to detect control chars
      if(char.IsControl(c))
      {
        sb.Append($"\\u{((int)c):X4}");
      }
      else
      {
        sb.Append(c);
      }
    }

    return sb.ToString();
  }

  public static string ToPrintableUtf8(byte[] bytes)
  {
    string text = Encoding.UTF8.GetString(bytes);
    return ToPrintable(text);
  }
}
