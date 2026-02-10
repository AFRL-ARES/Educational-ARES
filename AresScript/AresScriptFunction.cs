using AresScript.Generated;

namespace AresScript;

public record AresScriptFunction(string Name, IReadOnlyList<string> Parameters, AresLangParser.FuncBlockContext Body)
{
}
