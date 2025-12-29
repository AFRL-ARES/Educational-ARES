namespace ChemyxPumpPlugin.Commands.Responses;

public record SinglePumpParameters(PumpUnits Units, double Diameter, double Rate, TimeSpan Time, double Volume, TimeSpan Delay);