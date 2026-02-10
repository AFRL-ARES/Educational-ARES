using Ares.Datamodel;

namespace AresScript;

public abstract class LoopControlFlowException : Exception
{
  protected LoopControlFlowException()
  {
  }
}

public sealed class LoopBreakException : LoopControlFlowException
{
}

public sealed class LoopContinueException : LoopControlFlowException
{
}

public sealed class ReturnControlFlowException(AresValue value) : Exception
{
  public AresValue Value { get; } = value;
}
