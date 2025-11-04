using Ares.Datamodel.Templates;

namespace Ares.Core.AresEnvironment
{
  public static class AresEnvironment
  {
    public static void SetEnvironmentVariable(VariableType variableType, string value)
    {
      AresEnvironmentVariables[variableType] = value;
    }

    public static string? GetEnvironmentVariable(VariableType variableType)
    {
      return AresEnvironmentVariables.GetValueOrDefault(variableType);
    }

    public static void SetInternalVariable(InternalVariableType variableType, string value)
    {
      AresInternalVariables[variableType] = value;
    }

    public static string? GetInternalVariable(InternalVariableType variableType)
    {
      return AresInternalVariables.GetValueOrDefault(variableType);
    }

    private static Dictionary<VariableType, string> AresEnvironmentVariables { get; } = [];

    private static Dictionary<InternalVariableType, string> AresInternalVariables { get; } = [];
  }
}