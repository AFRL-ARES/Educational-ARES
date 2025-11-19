using DynamicData;
using System.Text;

namespace PrusaMK4S.Handlers;

public class GCodeHandler : IGcodeHandler
{
  private const int _printBedHeight = 210;
  private const int _printBedWidth = 190;
  private const int _distanceBetweenPurgeLines = 5;
  private static string[] _movementCommands = [ "G0", "G1", "G2", "G3" ];

  public GCodeHandler(byte[] original_data)
  {
    OriginalFileData = original_data;
  }

  public async Task Init()
  {
    ModificationStartIndex = -1;
    await CalculateObjectSize();
    AdjustedFileData = await GenerateFirstPrintData();
  }

  public async Task<byte[]> ApplyPlanningParameters(int bedTemperature, 
    int nozzleTemperature, 
    double extrusionMod,
    double speedMod, 
    double retractionLength, 
    double accelerationMod,
    double fanSpeedMod,
    byte[] gcode)
  {
    var data = await ConvertGCodeToStrings(gcode);
    //data = UpdateBedLeveling(data, 0);
    DetermineModificationIndex(data);

    // The G-Code associated with the user defined main object
    List<string> main_print_gcode;
    // Start G-Code contains the nozzle and bed temperature, which we might need to edit sometimes
    List<string> start_gcode = data.Take(ModificationStartIndex + 1).ToList();

    main_print_gcode = data.Skip(ModificationStartIndex + 1).ToList();
    
    if(nozzleTemperature > 0)
      start_gcode = UpdateNozzleTemperature(nozzleTemperature, start_gcode);

    if(bedTemperature > 0)
      start_gcode = UpdateBedTemperature(bedTemperature, start_gcode);

    if(extrusionMod > 0)
      main_print_gcode = UpdateExtrusionRate(extrusionMod, main_print_gcode);

    if(speedMod > 0)
      main_print_gcode = UpdateMovementSpeed(speedMod, main_print_gcode);

    if(retractionLength > 0)
      main_print_gcode = UpdateRetractionLength(retractionLength, main_print_gcode);

    if(accelerationMod > 0)
      main_print_gcode = UpdateAcceleration(accelerationMod, main_print_gcode);

    if(fanSpeedMod > 0)
      main_print_gcode = UpdateFanSpeed(fanSpeedMod, main_print_gcode);

    var updated_gcode = ConvertGCodeToBytes(start_gcode, main_print_gcode);
    return updated_gcode;
  }

  private List<string> UpdateExtrusionRate(double modifier, List<string> gcode)
  {
    var updatedData = new List<string>();

    foreach(var entry in gcode)
    {
      if(entry.StartsWith("G"))
      {
        var splitString = entry.Split();

        //All non-movement based G code commands
        if(!_movementCommands.Contains(splitString[0]))
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

    return updatedData;
  }

  private List<string> UpdateMovementSpeed(double modifier, List<string> gcode)
  {
    var updatedData = new List<string>();

    foreach(var entry in gcode)
    {
      if(entry.StartsWith("G"))
      {
        var splitString = entry.Split();

        //All non-movement based G code commands
        if(!_movementCommands.Contains(splitString[0]))
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

    return updatedData;
  }

  private List<string> UpdateAcceleration(double accelerationMod, List<string> gcode)
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

    return stringData;
  }

  private List<string> UpdateRetractionLength(double retractionLength, List<string> gcode)
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

    return gcode;
  }

  private List<string> UpdateNozzleTemperature(int desiredTemp, List<string> gcode)
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

    return updatedData;
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

  private List<string> UpdateBedTemperature(int desiredTemp, List<string> gcode)
  {
    var updatedData = gcode.Select(entry =>
    {
      if(entry.StartsWith("M140 S"))
        return $"M140 S{desiredTemp}";

      if(entry.StartsWith("M190 S"))
        return $"M190 S{desiredTemp}";

      return entry;
    }).ToList();

    return updatedData;
  }

  private List<string> UpdateFanSpeed(double fanSpeedMod, List<string> gcode)
  {
    var updatedData = new List<string>();
    
    foreach(var line in gcode)
    {
      if(line.StartsWith("M106"))
      {
        var tokens = line.Split();

        if(tokens.Length < 2 || !tokens[1].StartsWith("S", StringComparison.OrdinalIgnoreCase))
        {
          updatedData.Add(line);
          continue;
        }

        var fanToken = tokens[1];
        var parsedSpeed = double.TryParse(fanToken.Substring(1), out var speed);

        if(!parsedSpeed)
        {
          updatedData.Add(line);
          continue;
        }

        var newSpeed = speed * fanSpeedMod;
        newSpeed = Math.Clamp(newSpeed, 0, 255);

        var newLine = $"M106 S{newSpeed}";
        updatedData.Add(newLine);
      }

      else
        updatedData.Add(line);
    }

    return updatedData;
  }

  public Task<uint> SmartDetermineNumberOfPrints()
  {
    var max_vertical = (int)Math.Floor(_printBedHeight / Math.Max(ItemHeight + 10, 20));
    return Task.FromResult((uint)max_vertical);
  }

  public async Task<byte[]> CreatePrintIteration(int iteration)
  {
    if(AdjustedFileData is null)
      await Init();

    if(iteration == 1 && AdjustedFileData is not null)
    {
      return AdjustedFileData;
    }

    else
    {
      var numObjects = Math.Floor(_printBedWidth / (ItemWidth * 1.5));
      iteration--;
      var xShift = CalculateXShift(iteration, numObjects);
      var yShift = CalculateYShift(iteration);
      LatestXShift = xShift;
      LatestYShift = yShift;

      if(Math.Abs(yShift) >= _printBedHeight - ItemHeight - 5)
        return Array.Empty<byte>();

      var stringData = await ConvertGCodeToStrings(AdjustedFileData!);
      stringData = UpdateBedLeveling(stringData, iteration);
      DetermineModificationIndex(stringData);

      var startup_gcode = stringData.Take(ModificationStartIndex).ToList();
      startup_gcode = ShiftIterationPurgeLine(startup_gcode, iteration);

      var modifiedData = startup_gcode
      .Concat(stringData
        .Skip(ModificationStartIndex)
        .Select(line => ApplyOffsetToLine(line, xShift, yShift, 0)));

      var modifiedStringData = string.Join("\n", modifiedData);
      var data = Encoding.UTF8.GetBytes(modifiedStringData);
      return data;
    }
  }

  private double CalculateXShift(int itemIndex, double numObjects)
  {
    var position = itemIndex % numObjects;

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

  private string? ApplyOffsetToLine(string line, double xOffset, double yOffset, int iteration)
  {
    if(string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
      return line;

    if(line.StartsWith("G"))
    {
      var splitLine = line.Split(' ');
      var command = splitLine[0];

      //Check if our command is a movment related command.
      if(_movementCommands.Contains(command) && line.Contains("X") || line.Contains("Y"))
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
        if(_movementCommands.Contains(command) && line.Contains("X") || line.Contains("Y"))
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
    DetermineModificationIndex(stringData);

    var startup_gcode = stringData.Take(ModificationStartIndex).ToList();
    var shifted_startup = ShiftInitialPurgeLine(startup_gcode);
    shifted_startup = UpdateBedLeveling(shifted_startup, 0);

    var modifiedData = shifted_startup
      .Concat(stringData
        .Skip(ModificationStartIndex)
        .Select(line => ApplyOffsetToLine(line, XMinimumOffset, YMaximumOffset, 0)));

    var modifiedStringData = string.Join("\n", modifiedData);
    var bytes = Encoding.UTF8.GetBytes(modifiedStringData);
    await File.WriteAllBytesAsync("originalModifications.gcode", bytes);
    return bytes;
  }

  private List<string> ShiftInitialPurgeLine(List<string> gcode)
  {
    var updatedGcode = new List<string>(); 
    var shouldEdit = false;

    foreach(var line in gcode)
    {
      //if(line.Contains("; probe near purge place"))
      //  updatedGcode.Add(ApplyPurgeXOffset(line.Split(), 200));

      if(line.StartsWith("; prepare for purge"))
      {
        shouldEdit = true;
        updatedGcode.Add(line);
      }

      else if(line.StartsWith("G") && shouldEdit)
      {
        var updatedLine = ApplyPurgeXOffset(line.Split(), 200);
        updatedGcode.Add(updatedLine);
      }

      else
        updatedGcode.Add(line);
    }

    return updatedGcode;
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

  private List<string> ShiftIterationPurgeLine(List<string> gcode, int iteration)
  {
    var yShift = iteration * _distanceBetweenPurgeLines;
    var updatedGcode = new List<string>();
    var shouldEdit = false;

    foreach(var line in gcode)
    {
      //if(line.Contains("; probe near purge place"))
        //updatedGcode.Add(ApplyPurgeYOffset(line.Split(), yShift));

      if(line.StartsWith("; prepare for purge"))
      {
        shouldEdit = true;
        updatedGcode.Add(line);
      }

      else if(line.StartsWith("G") && shouldEdit)
        updatedGcode.Add(ApplyPurgeYOffset(line.Split(), yShift));

      else
        updatedGcode.Add(line);
    }

    return updatedGcode;
  }

  private string ApplyPurgeYOffset(string[] splitLine, int shift)
  {
    var y = splitLine.FirstOrDefault(e => e.StartsWith("Y"));
    //var height = splitLine.FirstOrDefault(e => e.StartsWith("H"));
    //var width = splitLine.FirstOrDefault(e => e.StartsWith("W"));

    //if(splitLine[0] == "G29")
    //{
      //var height_index = splitLine.IndexOf(height);
      //var width_index = splitLine.IndexOf(width);

      //splitLine[height_index] = "H25";
      //splitLine[width_index] = "W25";
    //}

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

  private List<string> UpdateBedLeveling(List<string> gcode, int printIteration)
  {
    List<string> updated_gcode = new List<string>();

    foreach(var line in gcode)
    {
      if(line.StartsWith("G29 P1"))
      {
        if(line.Contains("purge"))
        {
          updated_gcode.Add(line);
        }

        else
        {
          var x_center = LatestXShift + (0.5 * ItemWidth);
          var y_center = _printBedHeight + LatestYShift - (0.5 * ItemHeight);
          var newLevelCommand = $"G29 P1 X{x_center} Y{y_center} W{ItemWidth} H{ItemHeight} ; Level the ares determined by ARES"; 
          updated_gcode.Add(newLevelCommand);
        }
      }

      else
        updated_gcode.Add(line);
    }

    return updated_gcode;
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

  private byte[] ConvertGCodeToBytes(List<string> start_gcode, List<string> updated_gcode)
  {
    updated_gcode.ForEach(start_gcode.Add);
    var modifiedStringData = string.Join("\n", start_gcode);
    return Encoding.UTF8.GetBytes(modifiedStringData);
  }

  private void DetermineModificationIndex(List<string> gcode)
  {
    var index = 0;

    foreach(var line in gcode)
    {
      if(line.StartsWith("M221 S100"))
        ModificationStartIndex = index;

      index++;
    }
  }

  public int GetPrintBedHeight() => _printBedHeight;
  public int GetPrintBedWidth() => _printBedWidth;

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
  public bool SearchForMinAndMax { get; set; }
  public double LatestXShift { get; set; }
  public double LatestYShift { get; set; }
  public int ModificationStartIndex { get; set; } = -1;
}
