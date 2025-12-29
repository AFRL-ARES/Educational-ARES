using System;

namespace Ares.Device;
public interface IAresDeviceConnection : IAsyncDisposable
{
  string? Name { get; }
  bool IsOpen { get; }
}
