using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Ares.Device.Serial.Commands;

namespace Ares.Device.Serial;

public abstract class AresSerialConnection : IAresSerialConnection
{
  private readonly List<SerialBlock> _buffer = [];
  private readonly ManualResetEventSlim _bufferEvent = new();
  private readonly object _bufferLock = new();
  private readonly CancellationTokenSource _listenerCancellationTokenSource = new();
  private readonly IList<ISerialCommandWithResponse> _multiResponseQueue = [];
  private readonly ISubject<(ISerialCommandWithResponse, SerialResponse)> _responsePublisher = new Subject<(ISerialCommandWithResponse, SerialResponse)>();
  private readonly TimeSpan _sendBuffer;
  private readonly TimeSpan _defaultTimeout;
  private readonly TimeSpan _staleBufferEntryDuration;
  private readonly SemaphoreSlim _sendLock = new(1);
  private readonly IList<ISerialCommandWithResponse> _singleResponseQueue = [];

  /// <summary>
  /// Creates a new serial connection.
  /// </summary>
  /// <param name="connectionInfo">Information about the serial connection needed to connect</param>
  /// <param name="portName">Name of the port</param>
  /// <param name="sendBuffer">
  /// Can be set if there needs to be a buffer between each command send. Can be helpful
  /// if there are multiple devices on one connection and they get overwhelmed with commands from each other.
  /// </param>
  protected internal AresSerialConnection(SerialPortConnectionInfo connectionInfo, string portName, SerialConnectionOptions? options = null)
  {
    _sendBuffer = options?.SendBuffer ?? TimeSpan.Zero;
    _defaultTimeout = options?.SendTimeout ?? TimeSpan.FromDays(10);
    _staleBufferEntryDuration = options?.StaleBufferEntryDuration ?? TimeSpan.FromSeconds(10);
    ConnectionInfo = connectionInfo;
    Name = portName;
    StartBufferProcessor();
  }

  protected SerialPortConnectionInfo ConnectionInfo { get; }

  public void AttemptOpen()
  {
    Open(Name);
    if(!IsOpen)
      throw new InvalidOperationException($"Successfully executed Open on {Name}, but did not report IsOpen");

    Listen();
  }

  public async Task<T> Send<T>(SerialCommandWithResponse<T> command, TimeSpan timeout, Func<T, bool>? filter) where T : SerialResponse
  {
    if(command is SerialCommandWithStreamedResponse<T>)
      throw new InvalidOperationException(
        "Attempted to send a command for a streamed response. Call Send instead");

    lock(_singleResponseQueue)
    {
      _singleResponseQueue.Add(command);
    }

    var getResponseTask =
      GetTransactionStream<T>()
        .Where(transaction => filter?.Invoke(transaction.Response) ?? transaction.Request == command)
        .Take(1)
        .Select(transaction => transaction.Response)
        .Timeout(timeout)
        .Catch(Observable.Return<T?>(null))
        .ToTask();

    await _sendLock.WaitAsync();
    T? response;
    try
    {
      SendOutboundMessage(command);
      response = await getResponseTask;
      if(_sendBuffer > TimeSpan.Zero)
        await Task.Delay(_sendBuffer);
    }
    finally
    {
      _sendLock.Release();
      lock(_singleResponseQueue)
      {
        _singleResponseQueue.Remove(command);
      }
    }

    if(response is null)
      throw new TimeoutException($"Receiving message of type {typeof(T).Name} timed out");

    return response;
  }

  public Task<T> Send<T>(SerialCommandWithResponse<T> command, TimeSpan timeout) where T : SerialResponse
    => Send(command, timeout, null);

  public Task<T> Send<T>(SerialCommandWithResponse<T> command, Func<T, bool> filter) where T : SerialResponse
    => Send(command, _defaultTimeout, filter);

  public Task<T> Send<T>(SerialCommandWithResponse<T> command) where T : SerialResponse
    => Send(command, _defaultTimeout);

  public async Task<IObservable<T>> Send<T>(SerialCommandWithStreamedResponse<T> command) where T : SerialResponse
  {
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
      Console.WriteLine("Creating observer");
      var subscription = replay.Subscribe(observer);

      return Disposable.Create(() =>
      {
        subscription.Dispose();

        if(replay.HasObservers == false)
        {
          Console.WriteLine("Disposing observer");
          upstream.Dispose();
          replay.Dispose();

          lock(_multiResponseQueue)
          {
            _multiResponseQueue.Remove(command);
          }
        }
      });
    })
      .Replay()
      .RefCount();

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

  public virtual void Dispose()
  {
    _listenerCancellationTokenSource.Cancel();
    Close();
    _bufferEvent.Dispose();
    _listenerCancellationTokenSource.Dispose();
    _sendLock.Dispose();
  }

  private void StartBufferProcessor()
  {
    Task.Factory.StartNew(_ =>
    {
      try
      {
        while(!_listenerCancellationTokenSource.Token.IsCancellationRequested)
          ProcessBufferCore();
      }
      // TODO maybe there's a cleaner way to do this 
      catch(ObjectDisposedException)
      {
      }
    },
      _listenerCancellationTokenSource.Token,
      TaskCreationOptions.LongRunning);
  }

  protected internal abstract void CloseCore();

  public virtual void Listen()
  {
  }

  public virtual void StopListening()
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
      lock(_singleResponseQueue)
        lock(_multiResponseQueue)
        {
          if(_buffer.Any())
          {
            var currentData = _buffer.ToArray();
            var unparsedMultiParsers = _multiResponseQueue.Where(multiResponseCmd => _singleResponseQueue.All(singleResponseCmd => singleResponseCmd.ResponseParser.GetType() != multiResponseCmd.ResponseParser.GetType())).ToArray();
            var considerableParsers = _singleResponseQueue.Concat(unparsedMultiParsers);
            var parsedResponses = considerableParsers.Select(cmd =>
            {
              var parsed = cmd.ResponseParser.TryParseResponse(currentData, out var response, out var dataToRemove);
              if(parsed && response is not null)
                response.RequestId = cmd.Id;

              return (Parsed: parsed, Response: response, DataToRemove: dataToRemove, CommandToRemove: cmd);
            }).Where(proxy => proxy.Parsed && proxy.DataToRemove is not null).ToArray();

            // TODO check for overlapping arrays maybe?
            var orderedArraySegs = parsedResponses.Select(tuple => tuple.DataToRemove)
              .OrderBy(bytes => bytes!.Value.Offset)
              .Distinct();

            foreach(var arrSegment in orderedArraySegs)
            {
              _buffer.RemoveBytes(arrSegment!.Value);
              totalBytesRemoved += arrSegment.Value.Count;
            }

            foreach(var (Parsed, Response, DataToRemove, CommandToRemove) in parsedResponses)
              if(Parsed)
                _responsePublisher.OnNext((CommandToRemove, Response!));
          }

          RemoveStaleBufferEntries();
        }

    if(totalBytesRemoved == 0)
      _bufferEvent.Reset();
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
    }

    _bufferEvent.Set();
  }

  protected abstract void Open(string portName);

  internal bool BufferEmpty => _buffer.Count == 0;
}
