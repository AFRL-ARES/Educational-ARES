using DynamicData;
using System.Text;

namespace PrusaMK4S.Handlers;

public class GCodeHandler : IGcodeHandler
{
  public GCodeHandler(byte[] original_data)
  {
    OriginalFileData = original_data;
  }

  public async Task Init()
  {
    ModificationStartIndex = -1;
    ModificationStopIndex = -1;
    await CalculateObjectSize();
    AdjustedFileData = await GenerateFirstPrintData();
  }

  public async Task<byte[]> ApplyPlanningParameters(int bedTemperature, 
    int nozzleTemperature, 
    double extrusionMod,
    double speedMod, 
    double retractionLength, 
    double accelerationMod, 
    byte[] gcode)
  {
    var data = await ConvertGCodeToStrings(gcode);
    await DetermineModificationIndex(data);

    // The G-Code associated with the user defined main object
    List<string> main_print_gcode;
    // G-Code that comes at the end of a print, after the main object finishes, remains unmodified
    List<string> unmodified_end_gcode = new List<string>();
    // Start G-Code contains the nozzle and bed temperature, which we might need to edit sometimes
    List<string> start_gcode = data.Take(ModificationStartIndex + 1).ToList();


    if(ModificationStopIndex != -1)
    {
      var lengthOfMainPrint = ModificationStopIndex - ModificationStartIndex + 1;
      main_print_gcode = data.Skip(ModificationStartIndex + 1).Take(lengthOfMainPrint).ToList();
      unmodified_end_gcode = data.Skip(ModificationStopIndex + 1).ToList();
    }

    else
    {
      main_print_gcode = data.Skip(ModificationStartIndex + 1).ToList();
    }


    if(nozzleTemperature > 0)
      start_gcode = await UpdateNozzleTemperature(nozzleTemperature, start_gcode);

    if(bedTemperature > 0)
      start_gcode = await UpdateBedTemperature(bedTemperature, start_gcode);

    if(extrusionMod > 0)
      main_print_gcode = await UpdateExtrusionRate(extrusionMod, main_print_gcode);

    if(speedMod > 0)
      main_print_gcode = await UpdateMovementSpeed(speedMod, main_print_gcode);

    if(retractionLength > -1)
      main_print_gcode = await UpdateRetractionLength(retractionLength, main_print_gcode);

    if(accelerationMod > 0)
      main_print_gcode = await UpdateAcceleration(accelerationMod, main_print_gcode);

    var updated_gcode = await ConvertGCodeToBytes(start_gcode, main_print_gcode, unmodified_end_gcode);
    return updated_gcode;
  }

  private Task<List<string>> UpdateExtrusionRate(double modifier, List<string> gcode)
  {
    var updatedData = new List<string>();

    foreach(var entry in gcode)
    {
      if(entry.StartsWith("G"))
      {
        var splitString = entry.Split();

        //All non-movement based G code commands
        if(!MovementCommands.Contains(splitString[0]))
        {
          updatedData.Add(entry);
          continue;
        }

        var extrusionCommand = splitString.FirstOrDefault(e => e.StartsWith("E"));

        if(extrusionCommand is null || extrusionCommand.Length < 3)
        {
          updatedData.Add(entry);
          continue;
        }

        string extrusionRate;
        bool isDecimalRate;

        if(extrusionCommand.StartsWith("E."))
        {
          extrusionRate = $"0.{extrusionCommand.Substring(2)}";
          isDecimalRate = true;
        }

        else
        {
          extrusionRate = $"{extrusionCommand.Substring(1)}";
          isDecimalRate = false;
        }

        var parsed = float.TryParse(extrusionRate, out var floatExtrusionRate);

        if(!parsed)
          throw new InvalidOperationException();

        var newExtrusionRate = Math.Round(floatExtrusionRate * modifier, 5).ToString();
        string newExtrusionString;

        if(isDecimalRate)
          newExtrusionString = $"E.{newExtrusionRate.Substring(2)}";

        else
          newExtrusionString = $"E{newExtrusionRate}";

        var index = splitString.IndexOf(extrusionCommand);
        splitString[index] = newExtrusionString;

        var updatedLine = string.Join(" ", splitString);
        updatedData.Add(updatedLine);
      }

      else
        updatedData.Add(entry);
    }

    return Task.FromResult(updatedData);
  }

  private Task<List<string>> UpdateMovementSpeed(double modifier, List<string> gcode)
  {
    var updatedData = new List<string>();

    foreach(var entry in gcode)
    {
      if(entry.StartsWith("G"))
      {
        var splitString = entry.Split();

        //All non-movement based G code commands
        if(!MovementCommands.Contains(splitString[0]))
        {
          updatedData.Add(entry);
          continue;
        }

        var speedCommand = splitString.FirstOrDefault(e => e.StartsWith("F"));

        if(speedCommand is null || speedCommand.Length < 3)
        {
          updatedData.Add(entry);
          continue;
        }

        var speed = speedCommand.Substring(1);
        var parsed = float.TryParse(speed, out var floatSpeed);

        if(!parsed)
          throw new InvalidOperationException();

        var newSpeed = Math.Round(floatSpeed * modifier, 5).ToString();
        var newSpeedString = $"F{newSpeed}";

        var index = splitString.IndexOf(speedCommand);
        splitString[index] = newSpeedString;

        var updatedLine = string.Join(" ", splitString);

        updatedData.Add(updatedLine);
      }

      else
        updatedData.Add(entry);
    }

    return Task.FromResult(updatedData);
  }

  private Task<List<string>> UpdateAcceleration(double accelerationMod, List<string> gcode)
  {
    var stringData = new List<string>();

    foreach(var line in gcode)
    {
      if(line.StartsWith("M204"))
      {
        var splitLine = line.Split();
        var accelerationParam = splitLine.FirstOrDefault(cmd => cmd.StartsWith("P"));

        if(accelerationParam is null)
        {
          stringData.Add(line);
          continue;
        }

        var index = splitLine.IndexOf(accelerationParam);
        var parsed = int.TryParse(accelerationParam.Substring(1), out var intAcceleration);

        if(!parsed)
        {
          stringData.Add(line);
          continue;
        }

        var newAcceleration = (int)Math.Round(intAcceleration * accelerationMod);
        var newAccelerationString = $"P{newAcceleration}";

        splitLine[index] = newAccelerationString;
        var newCommand = string.Join(" ", splitLine);
        stringData.Add(newCommand);
      }

      else
      {
        stringData.Add(line);
      }
    }

    return Task.FromResult(stringData);
  }

  private Task<List<string>> UpdateRetractionLength(double retractionLength, List<string> gcode)
  {
    string? retractionCommand = null;
    var existingCommandIndex = -1;
    var insertionIndex = -1;

    var index = 0;
    foreach(var line in gcode)
    {
      if(line.StartsWith("M207"))
      {
        retractionCommand = line;
        existingCommandIndex = index;
      }

      if(line.StartsWith("M221 S100"))
      {
        insertionIndex = index;
      }

      index++;
    }

    if(retractionCommand is not null)
    {
      //Modify existing commmand
      var splitCommand = retractionCommand.Split();
      var length = splitCommand.IndexOf(splitCommand.FirstOrDefault(x => x.StartsWith("S")));
      splitCommand[length] = $"S{retractionLength}";

      var updatedCommand = string.Join(" ", splitCommand);
      gcode[existingCommandIndex] = updatedCommand;
    }

    else
    {
      var command = $"M207 S{retractionLength}";
      gcode.Insert(insertionIndex + 1, command);
    }

    return Task.FromResult(gcode);
  }

  private Task<List<string>> UpdateNozzleTemperature(int desiredTemp, List<string> gcode)
  {
    var updatedData = new List<string>();

    foreach(var entry in gcode)
    {
      if(MatchesNozzleTempCommand(entry))
      {
        var command = entry.Substring(0, 4);
        var updatedLine = $"{command} S{desiredTemp}";
        updatedData.Add(updatedLine);
      }

      else
        updatedData.Add(entry);
    }

    return Task.FromResult(updatedData);
  }

  private bool MatchesNozzleTempCommand(string entry)
  {
    if(entry.StartsWith("M104") || entry.StartsWith("M109"))
    {
      //End of print case, don't overwrite
      if(entry.Contains("S0"))
        return false;

      //Warmup gcode, don't edit
      if(entry.Contains("T0"))
        return false;

      return true;
    }

    return false;
  }

  private Task<List<string>> UpdateBedTemperature(int desiredTemp, List<string> gcode)
  {
    var updatedData = new List<string>();
    var bedTempMatchOne = $"M140 S{OriginalBedTemp}";
    var bedTempMatchTwo = $"M190 S{OriginalBedTemp}";

    foreach(var entry in gcode)
    {
      if(entry.StartsWith(bedTempMatchOne))
        updatedData.Add($"M140 S{desiredTemp}");

      else if(entry.StartsWith(bedTempMatchTwo))
        updatedData.Add($"M190 S{desiredTemp}");

      else
        updatedData.Add(entry);
    }

    return Task.FromResult(updatedData);
  }

  public Task<uint> SmartDetermineNumberOfPrints()
  {
    var max_vertical = (int)Math.Floor(PrintBedHeight / Math.Max(ItemHeight + 10, 20));
    return Task.FromResult((uint)max_vertical);
  }

  public async Task<byte[]> CreatePrintIteration(int iteration)
  {
    if(AdjustedFileData is null)
      await Init();

    if(iteration == 1)
      return AdjustedFileData!;

    else
    {
      var numObjects = Math.Floor(PrintBedWidth / (ItemWidth * 1.5));
      iteration--;
      var xShift = CalculateXShift(iteration, numObjects);
      var yShift = CalculateYShift(iteration);
      LatestXShift = xShift;
      LatestYShift = yShift;

      if(Math.Abs(yShift) >= PrintBedHeight - ItemHeight - 5)
        return Array.Empty<byte>();

      var stringData = await ConvertGCodeToStrings(AdjustedFileData!);
      stringData = await RemoveBedLeveling(stringData);
      //stringData = await RemoveRehomingSequence(stringData);
      await DetermineModificationIndex(stringData);

      var startup_gcode = stringData.Take(ModificationStartIndex).ToList();
      startup_gcode = await ShiftIterationPurgeLine(startup_gcode, iteration);

      var modifiedData = startup_gcode
      .Concat(stringData
        .Skip(ModificationStartIndex)
        .Select(line => UpdateLine(line, xShift, yShift, 0)));

      var modifiedStringData = string.Join("\n", modifiedData);
      var data = Encoding.UTF8.GetBytes(modifiedStringData);
      return data;
    }
  }

  private double CalculateXShift(int itemIndex, double numObjects)
  {
    var position = itemIndex % numObjects;

    //The first object in a row
    if(position == 0)
      return 0;

    else
      return (ItemWidth * 1.5) * position;
  }

  private double CalculateYShift(int itemIndex)
  {
    //Generally we can use the items height, but set a minimum distance.
    if(ItemHeight < 20)
      return -20 * itemIndex;

    else
      return (-ItemHeight - 10) * itemIndex;
  }

  private string? UpdateLine(string line, double xOffset, double yOffset, int iteration)
  {
    //Determine original desired nozzle temperature
    if(line.StartsWith("; temperature = "))
    {
      var temp = line.Substring(16);
      var parsed = int.TryParse(temp, out var intTemp);

      if(parsed)
        OriginalNozzleTemp = intTemp;

      return line;
    }

    //Determine original desired bed temperature
    if(iteration == 0 && line.StartsWith("; bed_temperature = "))
    {
      var temp = line.Substring(20);
      var parsed = int.TryParse(temp, out var intTemp);

      if(parsed)
        OriginalBedTemp = intTemp;

      return line;
    }

    if(string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
      return line;

    if(line.StartsWith("G"))
    {
      var splitLine = line.Split(' ');
      var command = splitLine[0];

      //Check if our command is a movment related command.
      if(MovementCommands.Contains(command) && line.Contains("X") || line.Contains("Y"))
      {
        var x = splitLine[1];
        var y = splitLine[2];
        var dataRemoved = command.Length + x.Length + y.Length + 3;

        var xMovementValue = x.Substring(1);
        var yMovementValue = y.Substring(1);

        var xDoubleMovementValue = double.Parse(xMovementValue);
        var yDoubleMovementValue = double.Parse(yMovementValue);

        xDoubleMovementValue = Math.Round(xDoubleMovementValue + xOffset, 3);
        yDoubleMovementValue = Math.Round(yDoubleMovementValue + yOffset, 3);

        if(line.Count() > dataRemoved)
          line = $"{command} X{xDoubleMovementValue} Y{yDoubleMovementValue} {line.Substring(dataRemoved)}";

        else
          line = $"{command} X{xDoubleMovementValue} Y{yDoubleMovementValue}";

        return line;
      }

      else if(command == "G29" && iteration != 1)
      {
        return string.Empty;
      }
    }
    return line;
  }

  private async Task CalculateObjectSize()
  {
    var memoryStream = new MemoryStream(OriginalFileData);
    var reader = new StreamReader(memoryStream, Encoding.UTF8);
    string? line;

    while((line = await reader.ReadLineAsync()) != null)
    {
      if(line is null)
        continue;

      if(line.Contains("M221 S100"))
        SearchForMinAndMax = true;

      if(line.Contains("; Filament-specific end gcode"))
        SearchForMinAndMax = false;

      if(line.StartsWith("G"))
      {
        var splitLine = line.Split(' ');
        var command = splitLine[0];

        //Check if our command is a movment related command.
        if(MovementCommands.Contains(command) && line.Contains("X") || line.Contains("Y"))
        {
          var x = splitLine[1];
          var y = splitLine[2];

          var xDoubleMovementValue = double.Parse(x.Substring(1));
          var yDoubleMovementValue = double.Parse(y.Substring(1));

          if(SearchForMinAndMax)
          {
            MinimumX = Math.Min(xDoubleMovementValue, MinimumX);
            MinimumY = Math.Min(yDoubleMovementValue, MinimumY);
            MaximumX = Math.Max(xDoubleMovementValue, MaximumX);
            MaximumY = Math.Max(yDoubleMovementValue, MaximumY);
          }
        }
      }
    }

    ItemWidth = Math.Ceiling(MaximumX - MinimumX);
    ItemHeight = Math.Ceiling(MaximumY - MinimumY);
    XMinimumOffset = 1 - MinimumX;
    YMaximumOffset = 205 - MaximumY;
    reader.Dispose();
    await memoryStream.DisposeAsync();
  }

  private async Task<byte[]> GenerateFirstPrintData()
  {
    if(OriginalFileData is null)
      return Array.Empty<byte>();

    var stringData = await ConvertGCodeToStrings(OriginalFileData);
    await DetermineModificationIndex(stringData);

    var startup_gcode = stringData.Take(ModificationStartIndex).ToList();
    var shifted_startup = await ShiftInitialPurgeLine(startup_gcode);
    //shifted_startup = await AddFullMeshBedLeveling(shifted_startup);

    var modifiedData = shifted_startup
      .Concat(stringData
        .Skip(ModificationStartIndex)
        .Select(line => UpdateLine(line, XMinimumOffset, YMaximumOffset, 0)));

    var modifiedStringData = string.Join("\n", modifiedData);
    var bytes = Encoding.UTF8.GetBytes(modifiedStringData);
    await File.WriteAllBytesAsync("originalModifications.gcode", bytes);
    return bytes;
  }

  private Task<List<string>> ShiftInitialPurgeLine(List<string> gcode)
  {
    var updatedGcode = new List<string>();
    var shouldEdit = false;

    foreach(var line in gcode)
    {
      if(line.StartsWith("; prepare for purge"))
        shouldEdit = true;

      if(line.StartsWith("G") && shouldEdit)
        updatedGcode.Add(ApplyPurgeXOffset(line.Split(), 200));

      else
        updatedGcode.Add(line);
    }

    return Task.FromResult(updatedGcode);
  }

  private string ApplyPurgeXOffset(string[] splitLine, int shift)
  {
    var x = splitLine.FirstOrDefault(e => e.StartsWith("X"));

    if(x is null)
    {
      var yParameter = $"X{shift}";
      splitLine.Append(yParameter);
    }

    else
    {
      var index = splitLine.IndexOf(x);
      var parsed = float.TryParse(x.Substring(1), out var xFloat);

      if(!parsed)
        return string.Join(" ", splitLine);

      var updatedX = $"X{xFloat + shift}";
      splitLine[index] = updatedX;
    }

    return string.Join(" ", splitLine);
  }

  private Task<List<string>> ShiftIterationPurgeLine(List<string> gcode, int iteration)
  {
    var yShift = iteration * 5;
    var updatedGcode = new List<string>();
    var shouldEdit = false;

    foreach(var line in gcode)
    {
      if(line.StartsWith("; prepare for purge"))
        shouldEdit = true;

      if(line.StartsWith("G") && shouldEdit)
        updatedGcode.Add(ApplyPurgeYOffset(line.Split(), yShift));

      else
        updatedGcode.Add(line);
    }

    return Task.FromResult(updatedGcode);
  }

  private string ApplyPurgeYOffset(string[] splitLine, int shift)
  {
    var y = splitLine.FirstOrDefault(e => e.StartsWith("Y"));

    if(y is null)
    {
      var yParameter = $"Y{shift}";
      splitLine.Append(yParameter);
    }

    else
    {
      var index = splitLine.IndexOf(y);
      var parsed = float.TryParse(y.Substring(1), out var yFloat);

      if(!parsed)
        return string.Join(" ", splitLine);

      var updatedY = $"Y{yFloat + shift}";
      splitLine[index] = updatedY;
    }

    return string.Join(" ", splitLine);
  }

  private Task<List<string>> RemoveBedLeveling(List<string> gcode)
  {
    List<string> updated_gcode = new List<string>();

    foreach (var line in gcode)
    {
      if (line.StartsWith("G29 P9"))
        updated_gcode.Add("G29 P9");

      else if (line.StartsWith("G29 P1") || line.StartsWith("G80"))
        continue;

      else
        updated_gcode.Add(line);
    }

    return Task.FromResult(updated_gcode);
  }
  
  private Task<List<string>> RemoveRehomingSequence(List<string> gcode)
  {
    List<string> updated_gcode = [];

    foreach (var line in gcode)
    {
      if (line.StartsWith("G28"))
        continue;

      updated_gcode.Add(line);
    }

    return Task.FromResult(updated_gcode);
  }

  private Task<List<string>> AddFullMeshBedLeveling(List<string> gcode)
  {
    List<string> updated_gcode = new List<string>();

    foreach(var line in gcode)
    {
      if(line.StartsWith("G29"))
        updated_gcode.Add("G80");

      else
        updated_gcode.Add(line);
    }

    return Task.FromResult(updated_gcode);
  }

  private async Task<List<string>> ConvertGCodeToStrings(byte[] gcode)
  {
    var memoryStream = new MemoryStream(gcode);
    var reader = new StreamReader(memoryStream, Encoding.UTF8);
    List<string> data = new List<string>();
    string? line;

    while((line = await reader.ReadLineAsync()) != null)
      data.Add(line);

    return data;
  }

  private Task<byte[]> ConvertGCodeToBytes(List<string> start_gcode, List<string> updated_gcode, List<string> end_gcode)
  {
    updated_gcode.ForEach(start_gcode.Add);
    end_gcode.ForEach(start_gcode.Add);
    var modifiedStringData = string.Join("\n", start_gcode);
    return Task.FromResult(Encoding.UTF8.GetBytes(modifiedStringData));
  }

  private Task DetermineModificationIndex(List<string> gcode)
  {
    var index = 0;

    foreach(var line in gcode)
    {
      if(line.StartsWith("M221 S100"))
        ModificationStartIndex = index;

      index++;
    }

    return Task.CompletedTask;
  }

  public ValueTask DisposeAsync()
  {
    return ValueTask.CompletedTask;
  }

  public byte[] OriginalFileData { get; private set; }
  public byte[]? AdjustedFileData { get; private set; }
  public double MinimumX { get; set; } = Double.PositiveInfinity;
  public double MaximumX { get; set; } = Double.NegativeInfinity;
  public double MinimumY { get; set; } = Double.PositiveInfinity;
  public double MaximumY { get; set; } = Double.NegativeInfinity;
  public double ItemHeight { get; set; }
  public double ItemWidth { get; set; }
  public double XMinimumOffset { get; set; }
  public double YMaximumOffset { get; set; }
  public int OriginalNozzleTemp { get; set; }
  public int OriginalBedTemp { get; set; }
  public double PrintBedHeight { get; } = 210;
  public double PrintBedWidth { get; } = 190;
  public string FileNameBase { get; } = "IterationPrint";
  public List<string> MovementCommands { get; } = new List<string>() { "G0", "G1", "G2", "G3" };
  public bool SearchForMinAndMax { get; set; }
  public double LatestXShift { get; set; }
  public double LatestYShift { get; set; }
  public int ModificationStartIndex { get; set; } = -1;
  public int ModificationStopIndex { get; set; } = -1;
}
