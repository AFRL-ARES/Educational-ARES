using Ares.Core.Device;
using AresCamera;
using AresCamera.Config;
using AresCamera.Services;
using AresService.DeviceManagers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.Services.Devices;

public class AresCameraService : AresCameraRpc.AresCameraRpcBase
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<AresCameraConfig, IAresCamera> _deviceManager;
  private readonly IDeviceConfigManager<AresCameraConfig> _configManager;

  public AresCameraService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDeviceManager<AresCameraConfig, IAresCamera> deviceManager,
    IDeviceConfigManager<AresCameraConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _deviceManager = deviceManager;
    _configManager = configManager;
  }

  private IAresCamera GetCamera(string name)
  {
    var camera = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IAresCamera>()
      .FirstOrDefault();

    if(camera is null)
      throw new InvalidOperationException($"Could not find Camera {name}");

    return camera;
  }

  public override async Task<ImageResponse> CaptureImage(CameraRequest request, ServerCallContext context)
  {
    var camera = GetCamera(request.CameraName);
    var image = await camera.CaptureImage();

    return new ImageResponse() { ImageData = ByteString.CopyFrom(image) };
  }

  public override async Task<Empty> UpdateCameraSource(AresCameraConfig request, ServerCallContext context)
  {
    var camera = GetCamera(request.DeviceName);
    await camera.UpdateSelectedDevice(request.SourceName);
    return new Empty();
  }

  public override async Task<AvailableSourcesResponse> UpdateAvailableSources(CameraRequest request, ServerCallContext context)
  {
    var camera = GetCamera(request.CameraName);
    var devices = await camera.GetAvailableDevices();

    var response = new AvailableSourcesResponse();
    response.AvailableSources.AddRange(devices);
    return response;
  }

  public override async Task<Empty> AddCamera(AresCameraConfig request, ServerCallContext context)
  {
    await _deviceManager.Load(request.DeviceId, request);
    var camera = GetCamera(request.DeviceName);
    await _configManager.Add(request.DeviceId, request.DeviceName, request);
    return new Empty();
  }

  public override async Task<Empty> RemoveCamera(CameraRequest request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.CameraName);
    await _configManager.Remove(request.CameraName);
    return new Empty();
  }

  public override Task<GetAllCamerasResponse> GetAllCameras(Empty request, ServerCallContext context)
  {
    var cameras = _deviceCommandInterpreterRepo
      .Select(deviceInterpreter => deviceInterpreter.Device)
      .OfType<IAresCamera>();

    var response = new GetAllCamerasResponse();
    response.Cameras.AddRange(cameras.Select(c => new CameraDescription { Id = c.UniqueId, Name = c.Name}));
    return Task.FromResult(response);
  }

  public override async Task<Empty> UpdateCamera(AresCameraConfig request, ServerCallContext context)
  {
    await _deviceManager.Update(request.DeviceId, request);
    await _configManager.Update(request.DeviceName, request);
    return new Empty();
  }
}
