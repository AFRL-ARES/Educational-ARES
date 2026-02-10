using Ares.Datamodel.Extensions;
using Ares.Datamodel;
using Ares.Datamodel.Factories;

namespace AresScript;

public static class StandardLibrary
{
  public static AresSystemFunction[] Functions { get; } =
  [
    new AresSystemFunction("print", (args, _) =>
      {
        foreach (var arg in args)
        {
          Console.WriteLine(arg.Stringify());
        }

        return Task.FromResult(AresValueHelper.CreateUnit());
      },
    AresSchemaBuilder.Create("args", AresDataType.Any).Build(),
    AresSchemaBuilder.Create(AresDataType.Unit).Build(),
    "Prints the given value/s of any ARES type to console."),
    
    new("range", (args, _) => {
      double start = 0;
      double stop = 0;
      double step = 1;

      if (args.Count == 1)
      {
        if (!args[0].HasNumberValue)
          throw new InvalidOperationException("Range argument must be a number.");
        stop = args[0].NumberValue;
      }
      else if (args.Count == 2)
      {
        if (!args[0].HasNumberValue || !args[1].HasNumberValue)
          throw new InvalidOperationException("Range arguments must be numbers.");
        start = args[0].NumberValue;
        stop = args[1].NumberValue;
      }
      else if (args.Count == 3)
      {
        if (!args[0].HasNumberValue || !args[1].HasNumberValue || !args[2].HasNumberValue)
          throw new InvalidOperationException("Range arguments must be numbers.");
        start = args[0].NumberValue;
        stop = args[1].NumberValue;
        step = args[2].NumberValue;
      }
      else
      {
        throw new InvalidOperationException($"Range expects 1, 2, or 3 arguments, got {args.Count}.");
      }

      if (Math.Abs(step) < double.Epsilon)
        throw new InvalidOperationException("Range step must not be zero.");

      var count = Math.Max(0, (int)Math.Ceiling((stop - start) / step));
      var numbers = Enumerable.Range(0, count)
                              .Select(i => start + (i * step))
                              .ToArray();

      return Task.FromResult(AresValueHelper.CreateNumberArray(numbers));
    },
    AresSchemaBuilder.Empty()
      .AddEntry("start", AresSchemaBuilder.NumberEntry().AsOptional().WithDescription("The starting number").Build())
      .AddEntry("stop", AresSchemaBuilder.NumberEntry().WithDescription("The non-inclusive stopping number.").Build())
      .AddEntry("step", AresSchemaBuilder.NumberEntry().AsOptional().WithDescription("The step size.").Build())
      .Build(),
    AresSchemaBuilder.Create("num_array", AresDataType.NumberArray).Build(),
    "Generates a list of numbers in a range."),
    
    new("sleep", async (args, token) => {
      if(args.Count != 1)
      {
        throw new ArgumentException("Expected exactly 1 argument for duration.", nameof(args));
      }

      if(!args[0].HasNumberValue)
      {
        throw new InvalidOperationException("Argument provided is not a number");
      }
      
      await Task.Delay(TimeSpan.FromMilliseconds(args[0].NumberValue), token);
      return AresValueHelper.CreateUnit();
    },
    AresSchemaBuilder.Create("time", AresDataType.Number)
      .WithDescription("Number of milliseconds to sleep")
      .WithUnit("ms")
      .Build(),
    AresSchemaBuilder.Create("", AresDataType.Unit).Build(),
    "Sleep for a given number of milliseconds"
    )
  ];
}
