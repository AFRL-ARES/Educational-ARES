using ChemyxPumpPlugin.Commands;
using ChemyxPumpPlugin.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChemyxPumpPlugin;

public static class UnitsProtoConverter
{
  public static Units ToProto(this PumpUnits units)
    => units switch
    {
        PumpUnits.MillilitersPerMinute => Units.MillilitersPerMinute,
        PumpUnits.MillilitersPerHour => Units.MillilitersPerHour,
        PumpUnits.MicroLitersPerMinute => Units.MicrolitersPerMinutes,
        PumpUnits.MicroLitersPerHour => Units.MicrolitersPerHour,
        _ => throw new NotImplementedException($"Unit of type {units} has not yet been implemented."),
    };

  public static PumpUnits FromProto(this Units units)
    => units switch
    {
        Units.Unknown => throw new InvalidOperationException("Unknown units selected :("),
        Units.MillilitersPerMinute => PumpUnits.MillilitersPerMinute,
        Units.MillilitersPerHour => PumpUnits.MillilitersPerHour,
        Units.MicrolitersPerMinutes => PumpUnits.MicroLitersPerMinute,
        Units.MicrolitersPerHour => PumpUnits.MicroLitersPerHour,
        _ => throw new NotImplementedException($"Unit of type {units} has not yet been implemented."),
    };
}
