using System;
using System.Threading.Tasks;
using Ares.Device.Serial.Commands;

namespace Ares.Device.Serial;

public interface IAresSerialConnection : IAresDeviceConnection
{
  void AttemptOpen();
  Task<T> Send<T>(SerialCommandWithResponse<T> command) where T : SerialResponse;
  Task<T> Send<T>(SerialCommandWithResponse<T> command, TimeSpan timeout) where T : SerialResponse;
  Task<T> Send<T>(SerialCommandWithResponse<T> command, TimeSpan timeout, Func<T, bool> filter) where T : SerialResponse;
  Task<T> Send<T>(SerialCommandWithResponse<T> command, Func<T, bool> filter) where T : SerialResponse;
  Task<IObservable<T>> Send<T>(SerialCommandWithStreamedResponse<T> command) where T : SerialResponse;
  IObservable<SerialTransaction<T>> GetTransactionStream<T>() where T : SerialResponse;
  Task Send(SerialCommand command);
  void Close();
}
