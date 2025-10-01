using Ares.Device.USB;
using MK4S.Config;
using MK4S.Services;

namespace PrusaMK4S;

public interface IPrusaMK4S : IAresUSBDevice, IAsyncDisposable
{
  IObservable<HttpResponseMessage?> StateStream { get; }
  Task<PrintTempsResponse> GetAndUpdateState();
  HttpResponseMessage? GetState();
  Task StartStateUpdater(TimeSpan interval);
  Task StartStateUpdater();
  Task StopStateUpdater();
  Task<uint> SmartCalculateNumberOfPrints(byte[] gcode);
  Task SetSmartPrintMode(bool smartPrintMode);
  void PopulateCredentials(MK4SConfig config);
  Task<MK4SRequestResponse> Print(byte[] gcode, string mainObjectName, int nozzleTemp, int bedTemp, double extrusionMod, double speedMod, int retractionLength, double accelerationMod);
  Task<MK4SRequestResponse> MovePrinter(string x, string y, string z, string dwell);
  Task<MK4SRequestResponse> MoveToLastPrint(string z, string dwell, int xOffset, int yOffset);
  Task<MK4SRequestResponse> HomePrinter();
  string Username { get; }
  string Password { get; }
  Uri? Address { get; }
  double BedTemperature { get; }
  double NozzleTemperature { get; }
}
