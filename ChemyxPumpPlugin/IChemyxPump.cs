using Ares.Device.Serial;
using ChemyxPumpPlugin.Commands;
using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin;

public interface IChemyxPump : ISerialDevice<IChemyxPumpConnection>, IAsyncDisposable
{
  Task StopPolling();

  void StartPolling();

  Task Start(int? pump = null, int mode = 0);

  Task Stop(int? pump = null);

  Task Pause(int? pump = null);

  PumpStatus? GetStatus(int? pump = null);

  double? GetDispensedVolume(int? pump = null);

  TimeSpan? GetElapsedTime(int? pump = null);

  LimitParameterResponse? ReadLimitParameter(int? pump = null);

  ViewParametersResponse? ViewParameters { get; }

  Task<double?> SetDiameter(double diameter, int? pump = null);

  Task<double?> SetRate(double rate, int? pump = null);

  Task<double?> SetVolume(double volume, int? pump = null);

  Task<double?> SetUnits(PumpUnits units, int? pump = null);

  Task<TimeSpan?> SetDelay(TimeSpan delay, int? pump = null);

  Task<(double rate, TimeSpan time)?> SetTime(TimeSpan time, int? pump = null);

  bool DualPump { get; set; }
}
