using Ares.Device.Serial.Commands;
using System;

namespace Ares.Device.Serial;
public record SerialConnectionOptions
{
  /// <summary>
  /// The default timeout for receiving a response for a <see cref="ISerialCommandWithResponse"/>
  /// Default 10 days
  /// </summary>
  public TimeSpan? SendTimeout { get; set; }

  /// <summary>
  /// Can be set if there needs to be a buffer of time between each command send. Can be helpful
  /// if there are multiple devices on one connection and they get overwhelmed with commands from each other.
  /// It will essentially wait the given amount of time after sending each command before allowing a new command through.
  /// Default no buffer
  /// </summary>
  public TimeSpan? SendBuffer { get; set; }

  /// <summary>
  /// How long can a block of bytes received from a serial port remain in the buffer before being removed.
  /// Will only have an effect if the device is providing data that is not being fully parsed and removed
  /// by the parser
  /// Default is 10 seconds
  /// </summary>
  public TimeSpan? StaleBufferEntryDuration { get; set; }
  
  /// <summary>
  /// Sometimes a command might time out waiting for a response and the ARES connection will open back up
  /// allowing new commands to be sent. In order to prevent internal buffer corruption, we wait a little bit
  /// to make sure the device has finished sending the data for a timed out command, and we wait until the last received
  /// bit of data happened further back than this value.
  /// Default is 50ms
  /// </summary>
  public TimeSpan? DataReceiveInterval { get; set; }
}
