using Ares.Device.Serial.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ares.Device.Serial;

public abstract class AresSerialConnection : IAresSerialConnection
{
  private readonly List<SerialBlock> _buffer = [];
  private readonly ManualResetEventSlim _bufferEvent = new();
  private readonly Lock _bufferLock = new();
  private readonly CancellationTokenSource _listenerCancellationTokenSource = new();
  private readonly IList<ISerialCommandWithResponse> _multiResponseQueue = [];
  private readonly ISubject<(ISerialCommandWithResponse, SerialResponse)> _responsePublisher = new Subject<(ISerialCommandWithResponse, SerialResponse)>();
  private readonly TimeSpan _sendBuffer;
  private readonly TimeSpan _receiveMargin;
  private Stopwatch _lastReceived = Stopwatch.StartNew();
  private readonly Task _bufferProcessor;
  private readonly TimeSpan _defaultTimeout;
  private readonly TimeSpan _staleBufferEntryDuration;
  private readonly SemaphoreSlim _sendLock = new(1);
  private readonly IList<ISerialCommandWithResponse> _singleResponseQueue = [];

  private int _pressure = 0;

  /// <summary>
  /// Creates a new serial connection.
  /// </summary>
  /// <param name="connectionInfo">Information about the serial connection needed to connect</param>
  /// <param name="portName">Name of the port</param>
  protected internal AresSerialConnection(SerialPortConnectionInfo connectionInfo, string portName, SerialConnectionOptions? options = null)
  {
    _sendBuffer = options?.SendBuffer ?? TimeSpan.Zero;
    _defaultTimeout = options?.SendTimeout ?? TimeSpan.FromDays(10);
    _staleBufferEntryDuration = options?.StaleBufferEntryDuration ?? TimeSpan.FromSeconds(10);
    _receiveMargin = options?.DataReceiveInterval ?? TimeSpan.FromMilliseconds(50);
    ConnectionInfo = connectionInfo;
    Name = portName;
    _bufferProcessor = StartBufferProcessor();
  }

  protected SerialPortConnectionInfo ConnectionInfo { get; }

  public void AttemptOpen()
  {
    Open(Name);
    if(!IsOpen)
      throw new InvalidOperationException($"Successfully executed Open on {Name}, but did not report IsOpen");

    Listen();
  }

  public async Task<T> Send<T>(SerialCommandWithResponse<T> command, TimeSpan timeout, CancellationToken token, Func<T, bool>? filter) where T : SerialResponse
  {
    if(command is SerialCommandWithStreamedResponse<T>)
      throw new InvalidOperationException(
        "Attempted to send a command for a streamed response. Call Send instead");

    Interlocked.Increment(ref _pressure);
    var getResponseTask =
      GetTransactionStream<T>()
        .Where(transaction => filter?.Invoke(transaction.Response) ?? transaction.Request == command)
        .Take(1)
        .Select(transaction => transaction.Response)
        .Timeout(timeout)
        //.Catch<T?, TimeoutException>(_ => Observable.Return<T?>(null))
        .ToTask(token);
    await _sendLock.WaitAsync(token);
    lock(_singleResponseQueue)
    {
      _singleResponseQueue.Add(command);
    }
    T? response = null;

    try
    {
      SendOutboundMessage(command);
      response = await getResponseTask;
      if(_sendBuffer > TimeSpan.Zero)
        await Task.Delay(_sendBuffer);
    }
    catch(TimeoutException)
    {
      await Task.Delay(_receiveMargin);

      // Wait until the device finishes sending data
      // We check how much time has passed since the last AddDataReceived call
      while(_lastReceived.Elapsed < _receiveMargin)
      {
        await Task.Delay(_receiveMargin);
      }
    }
    finally
    {
      lock(_singleResponseQueue)
      {
        _singleResponseQueue.Remove(command);
      }

      _sendLock.Release();
    }
    Interlocked.Decrement(ref _pressure);
    return response ?? throw new TimeoutException($"Receiving message of type {typeof(T).Name} timed out");

  }


  public static string ToPrintableUtf8(byte[] bytes)
  {
    string text = Encoding.UTF8.GetString(bytes);
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
  public Task<T> Send<T>(SerialCommandWithResponse<T> command, TimeSpan timeout) where T : SerialResponse
    => Send(command, timeout, CancellationToken.None, null);

  public Task<T> Send<T>(SerialCommandWithResponse<T> command, TimeSpan timeout, CancellationToken token) where T : SerialResponse
  => Send(command, timeout, token, null);

  public Task<T> Send<T>(SerialCommandWithResponse<T> command, Func<T, bool> filter) where T : SerialResponse
    => Send(command, _defaultTimeout, CancellationToken.None, filter);

  public Task<T> Send<T>(SerialCommandWithResponse<T> command) where T : SerialResponse
    => Send(command, _defaultTimeout);
  public Task<T> Send<T>(SerialCommandWithResponse<T> command, CancellationToken token) where T : SerialResponse
    => Send(command, _defaultTimeout, token, null);

  public Task<T> Send<T>(SerialCommandWithResponse<T> command, Func<T, bool> filter, CancellationToken token) where T : SerialResponse
    => Send(command, _defaultTimeout, token, filter);

  public async Task<IObservable<T>> SendAndStream<T>(SerialCommandWithStreamedResponse<T> command, CancellationToken? token) where T : SerialResponse
  {
    var ct = token ?? CancellationToken.None;
    lock(_multiResponseQueue)
    {
      var existingParser = _multiResponseQueue.OfType<SerialCommandWithStreamedResponse<T>>().FirstOrDefault();
      if(existingParser != null)
        _multiResponseQueue.Remove(existingParser);

      _multiResponseQueue.Add(command);
    }

    var replay = new ReplaySubject<T>(bufferSize: 100, window: TimeSpan.FromSeconds(10));
    var upstream = GetTransactionStream<T>()
      .Select(t => t.Response)
      .Subscribe(replay);

    var observable = Observable.Create<T>(observer =>
    {
      var subscription = replay.Subscribe(observer);

      return Disposable.Create(() =>
      {
        subscription.Dispose();

        if(replay.HasObservers)
          return;

        upstream.Dispose();
        replay.Dispose();

        lock(_multiResponseQueue)
        {
          _multiResponseQueue.Remove(command);
        }
      });
    });

    await _sendLock.WaitAsync(ct);
    try
    {
      SendOutboundMessage(command);
      if(_sendBuffer > TimeSpan.Zero)
        await Task.Delay(_sendBuffer, ct);
    }
    finally
    {
      _sendLock.Release();
    }

    return observable;
  }


  public IObservable<SerialTransaction<T>> GetTransactionStream<T>() where T : SerialResponse
  {
    var observable = _responsePublisher
      .Where(response => response.Item2.GetType() == typeof(T))
      .Select(tuple => new SerialTransaction<T>((SerialCommandWithResponse<T>)tuple.Item1, (T)tuple.Item2))
      .ObserveOn(TaskPoolScheduler.Default);

    return observable;
  }

  public async Task Send(SerialCommand command)
  {
    await _sendLock.WaitAsync();
    try
    {
      SendOutboundMessage(command);
      if(_sendBuffer > TimeSpan.Zero)
        await Task.Delay(_sendBuffer);
    }
    finally
    {
      _sendLock.Release();
    }
  }

  public void Close()
  {
    StopListening();
    CloseCore();
    if(!IsOpen)
      return;

    throw new InvalidOperationException("Successfully executed Close, but did not report IsOpen as false");
  }

  public string Name { get; }
  public bool IsOpen { get; protected set; }

  private Task StartBufferProcessor()
  {
    return Task.Run(() =>
      {
        while(!_listenerCancellationTokenSource.Token.IsCancellationRequested)
          ProcessBufferCore();
      },
      _listenerCancellationTokenSource.Token);
  }

  protected abstract void CloseCore();

  protected virtual void Listen()
  {
  }

  protected virtual void StopListening()
  {
  }

  protected abstract void SendOutboundMessage(SerialCommand command);

  private void ProcessBufferCore()
  {
    try
    {
      _bufferEvent.Wait(_listenerCancellationTokenSource.Token);
    }
    catch(OperationCanceledException)
    {
      return;
    }
    var totalBytesRemoved = 0;
    lock(_bufferLock)
    {
      lock(_singleResponseQueue)
        lock(_multiResponseQueue)
        {
          RemoveStaleBufferEntries();
          if(_buffer.Any())
          {
            var unparsedMultiParsers = _multiResponseQueue.Where(multiResponseCmd => _singleResponseQueue.All(singleResponseCmd => singleResponseCmd.ResponseParser.GetType() != multiResponseCmd.ResponseParser.GetType())).ToArray();
            var considerableParsers = _singleResponseQueue.Concat(unparsedMultiParsers).ToArray();

            foreach(var commandWithResponse in considerableParsers)
            {
              var currentData = _buffer.ToArray();
              var parsed = commandWithResponse.ResponseParser.TryParseResponse(currentData, out var response, out var dataToRemove);
              if(dataToRemove is not null)
              {
                _buffer.RemoveBytes(dataToRemove.Value);
                totalBytesRemoved += dataToRemove.Value.Count;
              }

              if(!parsed || response is null)
              {
                continue;
              }

              _responsePublisher.OnNext((commandWithResponse, response));
            }
          }
        }
      if(totalBytesRemoved == 0)
      {
        _bufferEvent.Reset();
      }
    }
  }

  private void RemoveStaleBufferEntries()
  {
    _buffer.RemoveAll(block => DateTime.UtcNow - block.Timestamp > _staleBufferEntryDuration);
  }

  protected void AddDataReceived(byte[] dataReceived)
  {
    lock(_bufferLock)
    {
      _buffer.Add(new SerialBlock(dataReceived, DateTime.UtcNow));
      _lastReceived.Restart();
      _bufferEvent.Set();
    }
  }

  protected abstract void Open(string portName);

  internal bool BufferEmpty => _buffer.Count == 0;

  public virtual async ValueTask DisposeAsync()
  {
    await _listenerCancellationTokenSource.CancelAsync();
    await _bufferProcessor;
    Close();
    await CastAndDispose(_bufferEvent);
    await CastAndDispose(_listenerCancellationTokenSource);
    await CastAndDispose(_sendLock);

    return;

    static async ValueTask CastAndDispose(IDisposable resource)
    {
      if(resource is IAsyncDisposable resourceAsyncDisposable)
        await resourceAsyncDisposable.DisposeAsync();
      else
        resource.Dispose();
    }
  }
}
