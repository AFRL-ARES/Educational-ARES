using Ares.Device.USB;

namespace AresCamera;

public interface IAresCamera : IAresUSBDevice, IAsyncDisposable
{
  public string? SelectedSourceName { get; set; }

  Task<byte[]> CaptureImage();

  Task<List<string>> GetAvailableDevices();

  Task<bool> UpdateSelectedDevice(string deviceName);
}
