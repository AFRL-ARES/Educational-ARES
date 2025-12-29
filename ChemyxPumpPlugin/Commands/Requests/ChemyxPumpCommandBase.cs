using System.Text;
using Ares.Device.Serial.Commands;
using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Requests;

internal abstract class ChemyxPumpCommandBase<TResponse> : SerialCommandWithResponse<TResponse> where TResponse : SerialResponse
{
  private readonly string _commandText;

  protected ChemyxPumpCommandBase(string commandText, SerialResponseParser<TResponse> parser) : base(parser)
  {
    _commandText = commandText;
  }

  protected override byte[] Serialize()
  {
    var text = _commandText.EndsWith('\r') ? _commandText : $"{_commandText}\r";
    return Encoding.ASCII.GetBytes(text);
  }
}
