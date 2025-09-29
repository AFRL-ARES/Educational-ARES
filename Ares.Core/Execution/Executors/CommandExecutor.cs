using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Core.Execution.ControlTokens;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Execution.Executors;

public class CommandExecutor : IExecutor<CommandExecutionSummary, CommandExecutionStatus>
{
  private readonly Func<CancellationToken, Task<CommandResult>> _command;
  private readonly BehaviorSubject<CommandExecutionStatus> _stateSubject;

  public CommandExecutor(Func<CancellationToken, Task<CommandResult>> command, CommandTemplate template)
  {
    _command = command;
    Template = template;
    var executionStatus = new CommandExecutionStatus
    {
      CommandId = template.UniqueId,
      CommandName = template.Metadata.Name,
      DeviceName = template.Metadata.DeviceType,
      State = ExecutionState.Undefined
    };

    _stateSubject = new BehaviorSubject<CommandExecutionStatus>(executionStatus);

    ExperimentStatusObservable = _stateSubject.AsObservable();
  }

  public CommandTemplate Template { get; set; }

  public IObservable<CommandExecutionStatus> ExperimentStatusObservable { get; }
  public CommandExecutionStatus Status => _stateSubject.Value;
  public async Task<CommandExecutionSummary> Execute(ExecutionControlToken token)
  {
    Status.State = token.IsPaused ? ExecutionState.Paused : ExecutionState.Running;
    _stateSubject.OnNext(Status);
    if(token.IsPaused)
      try
      {
        token.WaitForResume();
      }
      catch(OperationCanceledException)
      {
      }

    if(token.IsCancelled)
    {
      Status.State = ExecutionState.Failed;
      _stateSubject.OnNext(Status);
      _stateSubject.OnCompleted();
      return ExecutorSummaryHelpers.CreateCommandExecutionSummary(Template, null, DateTime.UtcNow, DateTime.UtcNow);
    }

    var timeStarted = DateTime.UtcNow;
    var execInfo = new ExecutionInfo { TimeStarted = DateTime.UtcNow.ToTimestamp() };
    var result = await InternalExecute(token.CancellationToken);
    execInfo.TimeFinished = DateTime.UtcNow.ToTimestamp();

    if(result.AwaitUserInput)
      AwaitUserInput(token);

    else if(result.Success)
      Status.State = ExecutionState.Succeeded;


    else
      Status.State = ExecutionState.Failed;

    _stateSubject.OnNext(Status);
    _stateSubject.OnCompleted();

    return ExecutorSummaryHelpers.CreateCommandExecutionSummary(Template, result, timeStarted, DateTime.UtcNow);
  }

  private async Task<CommandResult> InternalExecute(CancellationToken token)
  {
    try
    {
      var result = await _command(token);
      return result;
    }
    catch(Exception e)
    {
      var result = new CommandResult() { Success = false, Error = e.Message };
      return result;
    }
  }

  private void AwaitUserInput(ExecutionControlToken executionToken)
  {
    executionToken.Pause();
    Status.State = ExecutionState.AwaitingUser;
    _stateSubject.OnNext(Status);
    executionToken.WaitForResume();
    Status.State = ExecutionState.Succeeded;
  }
}
