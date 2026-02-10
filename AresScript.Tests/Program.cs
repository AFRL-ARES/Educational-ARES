using System.Diagnostics;
using Antlr4.Runtime;
using Ares.Datamodel.Extensions;
using AresScript.Generated;
using NUnit.Framework;

namespace AresScript.Tests;

[TestFixture]
public class InterpreterTests
{
  private sealed class ThrowingLexerErrorListener : IAntlrErrorListener<int>
  {
    public void SyntaxError(
      TextWriter output,
      IRecognizer recognizer,
      int offendingSymbol,
      int line,
      int charPositionInLine,
      string msg,
      RecognitionException e)
    {
      throw new InvalidOperationException($"Syntax error at {line}:{charPositionInLine} - {msg}");
    }
  }

  private sealed class ThrowingParserErrorListener : BaseErrorListener
  {
    public override void SyntaxError(
      TextWriter output,
      IRecognizer recognizer,
      IToken offendingSymbol,
      int line,
      int charPositionInLine,
      string msg,
      RecognitionException e)
    {
      throw new InvalidOperationException($"Syntax error at {line}:{charPositionInLine} - {msg}");
    }
  }

  private static async Task RunScriptAsync(string script, CancellationToken cancellationToken = default)
  {
    var stream = new AntlrInputStream(script);
    var lexer = new AresIndentationLexer(stream);
    lexer.RemoveErrorListeners();
    lexer.AddErrorListener(new ThrowingLexerErrorListener());
    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    parser.RemoveErrorListeners();
    parser.AddErrorListener(new ThrowingParserErrorListener());
    var programCtx = parser.program();
    var env = new Environment();
    env.AssignSystemFunctions(StandardLibrary.Functions);
    var visitor = new AresBaseInterpreter(env, cancellationToken);

    await visitor.Visit(programCtx);
  }

  private static async Task ValidateScriptAsync(string script)
  {
    var stream = new AntlrInputStream(script);
    var lexer = new AresIndentationLexer(stream);
    lexer.RemoveErrorListeners();
    lexer.AddErrorListener(new ThrowingLexerErrorListener());
    var tokenStream = new CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    parser.RemoveErrorListeners();
    parser.AddErrorListener(new ThrowingParserErrorListener());
    var programCtx = parser.program();
    var env = new Environment();
    env.AssignSystemFunctions(StandardLibrary.Functions);
    var visitor = new AresValidationInterpreter(env);

    await visitor.Visit(programCtx);
  }

  [Test]
  public async Task Assert_Passes_OnTrueCondition()
  {
    var script = """
      assert True
      assert 1 + 2 == 3
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public Task Assert_Fails_OnFalseCondition()
  {
    var script = """
      num1 = 20
      num2 = 40
      assert num1 + num2 == 80
      """;

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Assertion failed"));
    return Task.CompletedTask;
  }

  [Test]
  public Task Assert_Fails_OnNonBooleanCondition()
  {
    var script = """
      assert 123
      """;

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Assert condition must be boolean"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task KeywordArgs_BindCorrectly()
  {
    var script = """
      def add(a, b):
        return a + b

      assert add(1, 2) == 3
      assert add(a=1, b=2) == 3
      assert add(1, b=2) == 3
      assert add(b=2, a=1) == 3
      """;

    await RunScriptAsync(script);
  }

  [Test]
  public Task KeywordArgs_RejectPositionalAfterKeyword()
  {
    var script = """
      def add(a, b):
        return a + b

      add(a=1, 2)
      """;

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Positional argument follows keyword argument"));
    return Task.CompletedTask;
  }

  [Test]
  public Task KeywordArgs_RejectUnexpectedKeyword()
  {
    var script = """
      def add(a, b):
        return a + b

      add(c=1)
      """;

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("unexpected keyword argument"));
    return Task.CompletedTask;
  }

  [Test]
  public Task KeywordArgs_RejectMissingRequiredArgument()
  {
    var script = """
      def add(a, b):
        return a + b

      add(1)
      """;

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("missing required argument"));
    return Task.CompletedTask;
  }

  [Test]
  public Task KeywordArgs_RejectDuplicateKeyword()
  {
    var script = """
      def add(a, b):
        return a + b

      add(a=1, a=2)
      """;

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Duplicate keyword argument"));
    return Task.CompletedTask;
  }

  [Test]
  public Task KeywordArgs_RejectOnRuntimeFunctions()
  {
    var script = "print(value=1)";

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("does not support keyword arguments"));
    return Task.CompletedTask;
  }

  [Test]
  public Task SyntaxErrors_AreThrown()
  {
    var script = """
      if True
        print("oops")
      """;

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Syntax error"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task Range_OneArg_GeneratesCorrectSequence()
  {
    var script = """
      r = range(5)
      assert r[0] == 0
      assert r[1] == 1
      assert r[2] == 2
      assert r[3] == 3
      assert r[4] == 4
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Range_TwoArgs_GeneratesCorrectSequence()
  {
    var script = """
      r = range(2, 5)
      assert r[0] == 2
      assert r[1] == 3
      assert r[2] == 4
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Range_ThreeArgs_GeneratesCorrectSequence()
  {
    var script = """
      r = range(0, 10, 2)
      assert r[0] == 0
      assert r[1] == 2
      assert r[2] == 4
      assert r[3] == 6
      assert r[4] == 8
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Range_NegativeStep_GeneratesCorrectSequence()
  {
    var script = """
      r = range(5, 0, -1)
      assert r[0] == 5
      assert r[1] == 4
      assert r[2] == 3
      assert r[3] == 2
      assert r[4] == 1
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public Task Range_ZeroStep_ThrowsException()
  {
    var script = "range(0, 10, 0)";
    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Range step must not be zero"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task WhileLoop_ExecutesCorrectly()
  {
    var script = """
      i = 0
      sum = 0
      while i < 5:
        sum = sum + i
        i = i + 1
      assert sum == 10
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task ForLoop_ExecutesCorrectly()
  {
    var script = """
      sum = 0
      for i in range(5):
        sum = sum + i
      assert sum == 10
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task ForLoop_Break_StopsExecution()
  {
    var script = """
      sum = 0
      for i in range(10):
        if i == 5:
          break
        sum = sum + i
      assert sum == 10
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task ForLoop_Continue_SkipsIteration()
  {
    var script = """
      sum = 0
      for i in range(5):
        if i == 2:
          continue
        sum = sum + i
      assert sum == 8
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Nested_Return_Works()
  {
    var script = """
      def check(val):
        if val > 10:
          return True
        return False

      assert check(20) == True
      assert check(5) == False
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Struct_Member_Access_And_Assignment()
  {
    var script = """
      s = { "x": 10, "y": 20 }
      assert s.x == 10
      s.x = 30
      assert s.x == 30
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Struct_Member_Assignment_Creates_Missing_Field()
  {
    var script = """
      s = { }
      s.z = 42
      assert s.z == 42
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Validator_Does_Not_Execute_Runtime_Functions()
  {
    var script = """
      print("hi")
      """;

    var original = Console.Out;
    var output = new StringWriter();
    Console.SetOut(output);
    try
    {
      await ValidateScriptAsync(script);
    }
    finally
    {
      Console.SetOut(original);
    }

    Assert.That(output.ToString(), Is.Empty);
  }

  [Test]
  public Task Cancellation_Stops_Interpreter()
  {
    var script = "assert True";
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    var ex = Assert.ThrowsAsync<OperationCanceledException>(() => RunScriptAsync(script, cts.Token));
    Assert.That(ex, Is.Not.Null);
    return Task.CompletedTask;
  }

  [Test]
  public Task Validator_Rejects_Runtime_Keyword_Args()
  {
    var script = "print(value=1)";

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("does not support keyword arguments"));
    return Task.CompletedTask;
  }

  [Test]
  public Task Validator_Rejects_Positional_After_Keyword()
  {
    var script = """
      def add(a, b):
        return a + b

      add(a=1, 2)
      """;

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Positional argument follows keyword argument"));
    return Task.CompletedTask;
  }

  [Test]
  public Task Validator_Rejects_Missing_Function_At_Top_Level()
  {
    var script = "missing()";

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => ValidateScriptAsync(script));
    Assert.That(ex?.Message, Does.Contain("Function 'missing' not found"));
    return Task.CompletedTask;
  }

  [Test]
  public async Task Index_Assignment_Works_For_Number_Array()
  {
    var script = """
      nums = [1, 2, 3]
      nums[0] = 9
      assert nums[0] == 9
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Index_Assignment_Works_For_String_Array()
  {
    var script = """
      words = ["a", "b"]
      words[1] = "c"
      assert words[1] == "c"
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Index_Assignment_Works_For_List()
  {
    var script = """
      items = [1, "a"]
      items[0] = 2
      items[1] = "b"
      assert items[0] == 2
      assert items[1] == "b"
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Empty_Array_Literal_Does_Not_Throw()
  {
    var script = """
      empty = []
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public Task Empty_Array_Index_Access_Throws()
  {
    var script = """
                 empty_arr = []
                 test = empty_arr[1]
                 """;
    var ex = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => RunScriptAsync(script));
    return Task.CompletedTask;
  }

  [Test]
  public async Task Recursion_Works()
  {
    var script = """
      def fib(n):
        if n <= 1:
          return n
        return fib(n - 1) + fib(n - 2)

      assert fib(10) == 55
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Top_Level_Loop_Works()
  {
    var script = """
                 for i in range(10):
                   print(i + i)
                 """;

    await RunScriptAsync(script);
  }

  [Test]
  public async Task Inner_Scope_Shadows_Outer_Variable()
  {
    var script = """
      x = 1
      def inner():
        x = 2
        return x
      assert inner() == 2
      assert x == 1
      """;
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Block_At_Eof_Parses_Without_Trailing_Newline()
  {
    var script = "while False:\n  assert True";
    await RunScriptAsync(script);
  }

  [Test]
  public async Task Sleep_Test()
  {
    var script = "sleep(1000)";
    var stopwatch = Stopwatch.StartNew();
    await RunScriptAsync(script);
    stopwatch.Stop();
    Assert.That(stopwatch.ElapsedMilliseconds, Is.AtLeast(990));
  }

  [Test]
  [CancelAfter(5000)]
  public async Task Sleep_Cancel(CancellationToken testToken)
  {
    var script = "sleep(2000)";
    using var cts = new CancellationTokenSource();
    var executionTask = RunScriptAsync(script, cts.Token);
    await Task.Delay(TimeSpan.FromMilliseconds(500), testToken);
    await cts.CancelAsync();
    Assert.ThrowsAsync<TaskCanceledException>(() => executionTask);
  }

  [Test]
  public Task Too_Much_Recursion()
  {
    var script = """
                 def recurse(num):
                   if num <= 0:
                     return
                     
                   print(num)
                   recurse(num - 1)

                 recurse(101)
                 """;
    
    Assert.ThrowsAsync<InvalidOperationException>(() => RunScriptAsync(script));
    return Task.CompletedTask;
  }

  [Test]
  [Retry(5)]
  public async Task Parallel_Task()
  {
    var script = """
                 parallel:
                   sleep(200)
                   sleep(200)
                   sleep(200)
                   sleep(200)
                   sleep(200)
                 """;
    var stopwatch = Stopwatch.StartNew();
    await RunScriptAsync(script);
    stopwatch.Stop();

    // All sleeps together make up 1 second
    // parallelized they should take up a total of 200 + execution overhead
    Assert.That(stopwatch.ElapsedMilliseconds, Is.InRange(200, 400));
  }
}
