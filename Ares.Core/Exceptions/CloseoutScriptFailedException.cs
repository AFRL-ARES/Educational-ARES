namespace Ares.Core.Exceptions;

public class CloseoutScriptFailedException : Exception
{
  public string? Id { get; }
  public Type? ItemType { get; }
  public CloseoutScriptFailedException() { }
  public CloseoutScriptFailedException(string message) : base(message) { }
  public CloseoutScriptFailedException(string message, Exception innerException) : base(message, innerException) { }
  public CloseoutScriptFailedException(string id, Type itemType, string? message = null) 
    : base(message ?? $"A Campaign with ID '{id}' tried to execute it's closeout script, but it failed!")
  {
    Id = id;
    ItemType = itemType;
  }
}
