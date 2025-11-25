using System.Drawing;
using System.Drawing.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using Ares.Datamodel.Device;
using Ares.Device.USB;

namespace AresCamera;

public class AresCamera : AresUSBDevice, IAresCamera
{
  private FilterInfoCollection _availableDevices;
  private VideoCaptureDevice _videoCaptureDevice;

  public AresCamera(string name, string sourceName) : base(name)
  {
    _availableDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
    _videoCaptureDevice = Init(sourceName);
    _videoCaptureDevice.NewFrame += new NewFrameEventHandler(FrameCapturedEvent);
  }

  private VideoCaptureDevice Init(string sourceName)
  {
    FilterInfo? source = null;

    if(_availableDevices.Count == 0)
      return new VideoCaptureDevice();

    if(string.IsNullOrEmpty(sourceName))
      source = _availableDevices[0];

    else
    {
      foreach(FilterInfo device in _availableDevices)
      {
        if(device.Name == sourceName)
          source = device;
      }
      source ??= _availableDevices[0];
    }

    SelectedSourceName = source.Name;
    return new VideoCaptureDevice(source.MonikerString);
  }

  public Task<List<string>> GetAvailableDevices()
  {
    _availableDevices.Clear();
    _availableDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
    var deviceNames = new List<string>();

    for(int i = 0; i < _availableDevices.Count; i++)
      deviceNames.Add(_availableDevices[i].Name);

    return Task.FromResult(deviceNames);
  }

  public Task<bool> UpdateSelectedDevice(string deviceName)
  {
    var newDeviceIndex = -1;

    for(int i = 0; i < _availableDevices.Count; i++)
    {
      if(_availableDevices[i].Name == deviceName)
        newDeviceIndex = i;
    }

    if(newDeviceIndex == -1)
      return Task.FromResult(false);

    var newDevice = _availableDevices[newDeviceIndex];
    _videoCaptureDevice = new VideoCaptureDevice(newDevice.MonikerString);
    SelectedSourceName = newDevice.Name;
    _videoCaptureDevice.NewFrame += new NewFrameEventHandler(FrameCapturedEvent);
    return Task.FromResult(true);
  }

  public async Task<byte[]> CaptureImage()
  {
    if(!OperatingSystem.IsWindows())
      return Array.Empty<byte>();

    var cts = new CancellationTokenSource();
    cts.CancelAfter(10000);
    var token = cts.Token;

    if(_videoCaptureDevice.IsRunning)
    {
      var timeout = TimeSpan.FromSeconds(1.5);
      var timeoutTask = Task.Delay(timeout);
      var videoCaptureTask = Task.Run(() => _videoCaptureDevice.WaitForStop());
      var task = await Task.WhenAny(videoCaptureTask, timeoutTask);
      if(task == timeoutTask)
      {
        _videoCaptureDevice.Stop();
      }
    }

    //Let Camera Adjust
    await Task.Delay(TimeSpan.FromSeconds(2));
    var cameraControl = _videoCaptureDevice.SourceObject;
    _videoCaptureDevice.Start();

    while(!token.IsCancellationRequested && LatestImage is null)
      await Task.Delay(1000);

    cts.Dispose();
    if(LatestImage is null)
      return Array.Empty<byte>();

    _videoCaptureDevice.SignalToStop();

    byte[] imageBytes;

    using(var stream = new MemoryStream())
    {
      LatestImage.Save(stream, ImageFormat.Png);
      imageBytes = stream.ToArray();
    }

    await File.WriteAllBytesAsync("latest_image.png", imageBytes);

    LatestImage = null;
    return imageBytes;
  }

  private void FrameCapturedEvent(object sender, NewFrameEventArgs eventArgs)
  {
    if(!OperatingSystem.IsWindows())
      return;

    try
    {
      var bitmap = (Bitmap)eventArgs.Frame.Clone();
      LatestImage = bitmap;
    }

    catch(Exception ex)
    {
      Console.WriteLine($"Error displaying frame: {ex.Message}");
    }
  }

  public override Task<bool> Activate(CancellationToken ct)
  {
    Status = new DeviceOperationalStatus
    {
      OperationalState = OperationalState.Active,
      Message = "Activated Camera"
    };

    return Task.FromResult(true);
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  public ValueTask DisposeAsync()
  {
    if(OperatingSystem.IsWindows())
    {
      _availableDevices.Clear();
      _videoCaptureDevice.SignalToStop();
      LatestImage?.Dispose();
    }

    return ValueTask.CompletedTask;
  }

  public string? SelectedSourceName { get; set; }

  public Bitmap? LatestImage { get; set; }
}
