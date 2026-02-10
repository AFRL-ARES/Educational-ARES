using System.Threading.Tasks;
using Ares.Services;
using AresScript;
using Grpc.Core;
using System;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Grpc.Services;

public class AresScriptingService : Ares.Services.AresScriptingService.AresScriptingServiceBase
{
  private readonly ILogger<AresScriptingService> _logger;
  public AresScriptingService(ILogger<AresScriptingService> logger)
  {
    _logger = logger;
  }
  public override async Task ExecuteScript(ScriptExecutionRequest request, IServerStreamWriter<ScriptExecutionOutput> responseStream, ServerCallContext context)
  {
    var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(100));
    var runner = new ScriptRunner();
    runner.ScriptOutput.Subscribe(output =>
    {
      if (!channel.Writer.TryWrite(output))
      {
        _logger.LogWarning("Dropped script output because channel is full. {Output}", output);
      }
    });
    async Task ReadOutputAsync()
    {
      try
      {
        await foreach (var val in channel.Reader.ReadAllAsync(context.CancellationToken))
        {
          await responseStream.WriteAsync(new ScriptExecutionOutput { Output = val });
        }
      }
      catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
      {
        _logger.LogInformation("Grpc stream cancelled while sending script output.");
      }
      catch (RpcException e)
      {
        _logger.LogError("RpcException while trying to write to the grpc stream: {Exception}", e);
      }
      catch(Exception e)
      {
        _logger.LogError($"Exception while reading script output. {e}");
      }
    }
    var readTask = ReadOutputAsync();
    
    try
    {
      await runner.RunScriptAsync(request.Script, context.CancellationToken);
      channel.Writer.TryComplete();
    }
    catch (Exception e)
    {
      channel.Writer.TryComplete(e);
      _logger.LogError("Script runner failed: {Exception}", e);
      throw;
    }
    await readTask;
  }
}
