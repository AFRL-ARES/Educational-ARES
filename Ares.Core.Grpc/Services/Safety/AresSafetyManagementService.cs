using Ares.Core.Device;
using Ares.Core.Execution;
using Ares.Services;
using Grpc.Core;
using System;
using System.Threading.Tasks;

namespace Ares.Core.Grpc.Services.Safety;

public class AresSafetyManagementService : AresSafetyService.AresSafetyServiceBase
{
  private readonly IExecutionManager _executionManager;
  private readonly IDeviceCommandInterpreterRepo _deviceInterpreterRepo;

  public AresSafetyManagementService(IExecutionManager executionManager, IDeviceCommandInterpreterRepo deviceInterpreterRepo)
  {
    _executionManager = executionManager;
    _deviceInterpreterRepo = deviceInterpreterRepo;
  }

  public override Task<EmergencyStopResponse> RequestEmergencyStop(EmergencyStopRequest request, ServerCallContext context)
  {
    EmergencyStopResponse response;

    try
    {
      //Stop current campaign execution
      _executionManager.Stop();

      var deviceInterpreters = _deviceInterpreterRepo.GetEnumerator();
      while(deviceInterpreters.MoveNext())
      {
        var device = deviceInterpreters.Current.Device;
        device.EnterSafeMode();
      }

      response = new EmergencyStopResponse()
      {
        Status = EmergencyStopStatus.Success,
        StatusMessage = "Emergency stop successfully processed. Any running campaigns have been paused, and all devices have entered safe mode."
      };
    }

    catch(Exception e)
    {
      response = new EmergencyStopResponse()
      {
        Status = EmergencyStopStatus.Error,
        StatusMessage = e.Message
      };
    }

    return Task.FromResult(response);
  }
}
