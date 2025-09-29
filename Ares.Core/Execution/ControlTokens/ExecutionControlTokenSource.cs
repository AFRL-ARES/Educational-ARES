namespace Ares.Core.Execution.ControlTokens;

/// <summary>
/// Combination of a <see cref="PauseTokenSource" /> and a <see cref="CancellationTokenSource" /> so that we would only
/// need to pass in one object throughout the execution stack
/// </summary>
public class ExecutionControlTokenSource : IDisposable
{
  private readonly CancellationTokenSource _cancellationTokenSource = new();
  private readonly PauseTokenSource _pauseTokenSource = new();

  public CancellationToken CancellationToken => _cancellationTokenSource.Token;
  public PauseToken PauseToken => _pauseTokenSource.Token;

  public ExecutionControlToken Token => new(this);

  public void Dispose()
  {
    _cancellationTokenSource.Dispose();
    _pauseTokenSource.Dispose();
  }

  public void Pause()
  {
    _pauseTokenSource.Pause();
  }

  public void Cancel()
  {
    _cancellationTokenSource.Cancel();
  }

  public void Resume()
  {
    _pauseTokenSource.Resume();
  }

  public void WaitForResume()
  {
    _pauseTokenSource.Wait(CancellationToken);
  }
}
