using Ares.Device.Serial.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ares.Device.Serial;

public interface IAresSerialConnection : IAresDeviceConnection
{
  void AttemptOpen();
  Task<T> Send<T>(SerialCommandWithResponse<T> command) where T : SerialResponse;
  Task<T> Send<T>(SerialCommandWithResponse<T> command, TimeSpan timeout) where T : SerialResponse;
  Task<T> Send<T>(SerialCommandWithResponse<T> command, TimeSpan timeout, CancellationToken token) where T : SerialResponse;
  Task<T> Send<T>(SerialCommandWithResponse<T> command, CancellationToken token) where T : SerialResponse;
  Task<T> Send<T>(SerialCommandWithResponse<T> command, TimeSpan timeout, CancellationToken token, Func<T, bool> filter) where T : SerialResponse;
  Task<T> Send<T>(SerialCommandWithResponse<T> command, Func<T, bool> filter) where T : SerialResponse;
  Task<T> Send<T>(SerialCommandWithResponse<T> command, Func<T, bool> filter, CancellationToken token) where T : SerialResponse;
  Task<IObservable<T>> SendAndStream<T>(SerialCommandWithStreamedResponse<T> command, CancellationToken? token = null) where T : SerialResponse;
  IObservable<SerialTransaction<T>> GetTransactionStream<T>() where T : SerialResponse;
  Task Send(SerialCommand command);
  void Close();
}
