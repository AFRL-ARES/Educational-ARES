using Antlr4.Runtime.Misc;
using Ares.Datamodel.Extensions;
using AresScript.Generated;

namespace AresScript;

public sealed class AresValidationInterpreter : AresLangBaseVisitor<Task>
{
  private readonly Environment _locals = new();
  private int _functionDepth;

  public AresValidationInterpreter(Environment environment)
  {
    _locals = environment;
  }

  protected override Task DefaultResult => Task.CompletedTask;

  public override async Task VisitProgram(AresLangParser.ProgramContext context)
  {
    foreach(var child in context.children)
    {
      if(child is AresLangParser.StatementContext stmt)
      {
        await Visit(stmt);
      }
    }
  }

  public override async Task VisitBlock(AresLangParser.BlockContext context)
  {
    foreach(var stmt in context.statement())
    {
      await Visit(stmt);
    }
  }

  public override async Task VisitLoopBlock(AresLangParser.LoopBlockContext context)
  {
    foreach(var stmt in context.statement())
    {
      await Visit(stmt);
    }
  }

  public override async Task VisitFuncBlock(AresLangParser.FuncBlockContext context)
  {
    _locals.EnterScope();
    _functionDepth++;
    try
    {
      foreach(var stmt in context.statement())
      {
        await Visit(stmt);
      }
    }
    finally
    {
      _functionDepth--;
      _locals.ExitScope();
    }
  }

  public override async Task VisitParallelBlock([NotNull] AresLangParser.ParallelBlockContext context)
  {
    var expContexts = context.expression();
    var expTasks = expContexts.Select(Visit);
    await Task.WhenAll(expTasks);
  }

  public override async Task VisitAssignStmt(AresLangParser.AssignStmtContext context)
  {
    var assignment = context.assignment();
    await Visit(assignment.lvalue());
    await Visit(assignment.expression());

    if(assignment.lvalue() is AresLangParser.LValueIdContext idContext)
    {
      var id = idContext.ID().GetText();
      var functionId = TryResolveFunctionId(assignment.expression());
      if(functionId is not null && _locals.TryGetAresFunction(functionId, out var _)
          || functionId is not null && _locals.TryGetUserFunction(functionId, out var _))
      {
        _locals[id] = AresValueHelper.CreateFunction(functionId);
      }
      else if(functionId is not null && _locals.TryGetValue(functionId, out var value) && value.FunctionValue is not null)
      {
        _locals[id] = AresValueHelper.CreateFunction(value.FunctionValue.FunctionId);
      }
      else
      {
        _locals[id] = AresValueHelper.CreateNull();
      }
    }
  }

  public override async Task VisitExprStmt(AresLangParser.ExprStmtContext context)
  {
    await Visit(context.expression());
  }

  public override async Task VisitReturnStmt(AresLangParser.ReturnStmtContext context)
  {
    if(context.expression() is not null)
    {
      await Visit(context.expression());
    }
  }

  public override async Task VisitAssertStmt(AresLangParser.AssertStmtContext context)
  {
    var assertContext = context.assertStatement();
    foreach(var expr in assertContext.expression())
    {
      await Visit(expr);
    }
  }

  public override async Task VisitWhileStmt([NotNull] AresLangParser.WhileStmtContext context)
  {
    await Visit(context.whileStatement().expression());
    await Visit(context.whileStatement().loopBlock());
  }

  public override async Task VisitForStmt([NotNull] AresLangParser.ForStmtContext context)
  {
    await Visit(context.forStatement().expression());
    await Visit(context.forStatement().loopBlock());
  }

  public override async Task VisitFunctionDecl([NotNull] AresLangParser.FunctionDeclContext context)
  {
    var functionId = context.functionDeclaration().ID(0).GetText();
    var paramIds = context.functionDeclaration().ID()[1..].Select(p => p.GetText()).ToArray();
    var block = context.functionDeclaration().funcBlock();

    var userFunc = new AresScriptFunction(functionId, paramIds, block);
    _locals.AssignFunction(functionId, userFunc);

    await Visit(block);
  }

  public override async Task VisitFunctionCall(AresLangParser.FunctionCallContext ctx)
  {
    await Visit(ctx.expression());

    var positionalArgs = new List<AresLangParser.ExpressionContext>();
    var keywordArgs = new Dictionary<string, AresLangParser.ExpressionContext>(StringComparer.Ordinal);
    var seenKeywordArg = false;

    var argContexts = ctx.argList()?.argument() ?? Enumerable.Empty<AresLangParser.ArgumentContext>();
    foreach(var argCtx in argContexts)
    {
      switch(argCtx)
      {
        case AresLangParser.PositionalArgContext positionalArg:
          {
            if(seenKeywordArg)
            {
              throw new InvalidOperationException(
                $"Positional argument follows keyword argument. {positionalArg.Start.Line}:{positionalArg.Start.Column}"
              );
            }

            positionalArgs.Add(positionalArg.expression());
            await Visit(positionalArg.expression());
            break;
          }
        case AresLangParser.KeywordArgContext keywordArg:
          {
            seenKeywordArg = true;
            var name = keywordArg.ID().GetText();
            if(keywordArgs.ContainsKey(name))
            {
              throw new InvalidOperationException(
                $"Duplicate keyword argument '{name}'. {keywordArg.Start.Line}:{keywordArg.Start.Column}"
              );
            }

            keywordArgs[name] = keywordArg.expression();
            await Visit(keywordArg.expression());
            break;
          }
        default:
          throw new InvalidOperationException(
            $"Unsupported argument type {argCtx.GetType().Name}. {argCtx.Start.Line}:{argCtx.Start.Column}"
          );
      }
    }

    var functionId = TryResolveFunctionId(ctx.expression());
    if(functionId is null)
    {
      return;
    }

    if(_locals.TryGetValue(functionId, out var aliasValue) && aliasValue.FunctionValue is not null)
    {
      functionId = aliasValue.FunctionValue.FunctionId;
    }

    if(_locals.TryGetAresFunction(functionId, out var _))
    {
      if(keywordArgs.Count > 0)
      {
        throw new InvalidOperationException($"Runtime function '{functionId}' does not support keyword arguments");
      }
      return;
    }

    if(_locals.TryGetUserFunction(functionId, out var userFn))
    {
      if(positionalArgs.Count > userFn.Parameters.Count)
      {
        throw new InvalidOperationException(
          $"Function '{functionId}' expected {userFn.Parameters.Count} arguments but got {positionalArgs.Count}"
        );
      }

      foreach(var (name, _) in keywordArgs)
      {
        var index = FindParameterIndex(userFn.Parameters, name);
        if(index < 0)
        {
          throw new InvalidOperationException($"Function '{functionId}' got an unexpected keyword argument '{name}'");
        }

        if(index < positionalArgs.Count)
        {
          throw new InvalidOperationException($"Function '{functionId}' got multiple values for argument '{name}'");
        }
      }

      for(var i = positionalArgs.Count; i < userFn.Parameters.Count; i++)
      {
        var name = userFn.Parameters[i];
        if(!keywordArgs.ContainsKey(name))
        {
          throw new InvalidOperationException($"Function '{functionId}' missing required argument '{name}'");
        }
      }

      return;
    }

    if(_functionDepth == 0)
    {
      throw new InvalidOperationException($"Function '{functionId}' not found");
    }
  }

  private static int FindParameterIndex(IReadOnlyList<string> parameters, string name)
  {
    for(var i = 0; i < parameters.Count; i++)
    {
      if(string.Equals(parameters[i], name, StringComparison.Ordinal))
        return i;
    }

    return -1;
  }

  private static string? TryResolveFunctionId(AresLangParser.ExpressionContext expression)
  {
    var current = expression;
    while(true)
    {
      if(current is AresLangParser.AtomExprContext atomExpr)
      {
        switch(atomExpr.atom())
        {
          case AresLangParser.IdContext id:
            return id.ID().GetText();
          case AresLangParser.ParensContext parens:
            current = parens.expression();
            continue;
        }
      }

      return null;
    }
  }
}
