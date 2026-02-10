using Ares.Datamodel;

namespace AresScript;

public class ScriptScope(string name = "")
{
  public string Name { get; } = name;

  public Dictionary<string, AresValue> Variables { get; } = [];

  public Dictionary<string, AresScriptFunction> Functions { get; } = [];
}
