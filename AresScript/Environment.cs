using Ares.Datamodel;
using System.Diagnostics.CodeAnalysis;

namespace AresScript;

public class Environment
{
  private readonly Stack<ScriptScope> _scopes = [];

  public Environment()
  {
    var global = new ScriptScope("global");
    _scopes.Push(global);
  }

  public void AssignVariable(string id, AresValue value)
  {
    var currentScope = _scopes.Peek();
    currentScope.Variables[id] = value;
  }

  public void AssignFunction(string id, AresScriptFunction value)
  {
    if(AresFunctionExists(id))
    {
      throw new InvalidOperationException($"Function {id} already exists as a global function.");
    }

    var currentScope = _scopes.Peek();
    currentScope.Functions[id] = value;
  }

  public bool TryGetValue(string id, [NotNullWhen(true)] out AresValue? value)
  {
    foreach(var scope in _scopes)
    {
      var valueExists = scope.Variables.TryGetValue(id, out value);
      if(valueExists && value is not null)
        return true;
    }

    value = null;
    return false;
  }

  public bool TryGetValueCurrentScope(string id, [NotNullWhen(true)] out AresValue? value)
  {
    var scope = _scopes.Peek();
    return scope.Variables.TryGetValue(id, out value);
  }

  public bool TryGetUserFunction(string id, [NotNullWhen(true)] out AresScriptFunction? func)
  {
    foreach(var scope in _scopes)
    {
      var funcExists = scope.Functions.TryGetValue(id, out func);
      if(funcExists && func is not null)
        return true;
    }

    func = null;
    return false;
  }

  public bool TryGetAresFunction(string id, [NotNullWhen(true)] out AresSystemFunction? func)
  {
    return FunctionTable.TryGetValue(id, out func);
  }

  public bool AresFunctionExists(string id)
  {
    return FunctionTable.ContainsKey(id);
  }

  public int Depth => _scopes.Count;

  // Only for vars
  public AresValue this[string val]
  {
    get
    {
      if(TryGetValue(val, out var value))
        return value;

      throw new KeyNotFoundException($"Key {val} not found in the environment.");
    }
    set
    {
      AssignVariable(val, value);
    }
  }

  public void EnterScope(string name = "")
  {
    _scopes.Push(new ScriptScope(name));
  }

  public void ExitScope()
  {
    if(_scopes.Count <= 1)
    {
      throw new InvalidOperationException("Cannot exit the global scope.");
    }

    _scopes.Pop();
  }

  public void AssignSystemFunctions(IEnumerable<AresSystemFunction> functions)
  {
    foreach(var f in functions)
    {
      FunctionTable[f.Id] = f;
    }
  }

  // Global function table that maps function ids to internal system functions
  public Dictionary<string, AresSystemFunction> FunctionTable { get; } = [];
}
