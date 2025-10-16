namespace PrusaMK4S.Handlers;

public interface IGcodeHandler : IAsyncDisposable
{
  Task<byte[]> CreatePrintIteration(int iteration);
  Task<uint> SmartDetermineNumberOfPrints();
  Task<byte[]> ApplyPlanningParameters(int bedTemperature, int nozzleTemperature, double extrusionMod,
    double speedMod, double retractionLength, double accelerationMod, byte[] gcode);
  Task Init();
  public double MinimumX { get; }
  public double MaximumX { get; }
  public double MinimumY { get; }
  public double MaximumY { get; }
  public double ItemHeight { get; }
  public double ItemWidth { get; }
  public double XMinimumOffset { get; }
  public double YMaximumOffset { get; }
  public double PrintBedHeight { get; }
  public double PrintBedWidth { get; }
  public string FileNameBase { get; }
  public List<string> MovementCommands { get; }
  public bool SearchForMinAndMax { get; }
  public double LatestXShift { get; }
  public double LatestYShift { get; }
}
