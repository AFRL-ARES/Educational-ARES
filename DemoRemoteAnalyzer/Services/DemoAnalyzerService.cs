using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Connection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DemoRemoteAnalyzer.Services;
public class DemoAnalyzerService : AresRemoteAnalyzerService.AresRemoteAnalyzerServiceBase
{
  public DemoAnalyzerService()
  {
  }

  public override Task<StateResponse> GetState(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new StateResponse { State = State.Active, StateMessage = "The Demo Analyzer is Active!" });
  }

  public override Task<Analysis> Analyze(AnalysisRequest request, ServerCallContext context)
  {
    Console.WriteLine("Analysis requested");
    var numInput = request.Inputs.Fields[DemoDataTypes.InputNumber.Key];
    var input = numInput.NumberValue;
    Console.WriteLine($"Analysis input: {input}");
    var analysis = new Analysis
    {
      Result = (float)input,
      AnalysisOutcome = Outcome.Success,
    };

    var numOperand = request.Inputs.Fields.GetValueOrDefault(DemoDataTypes.Operand.Key);
    if(numOperand is null)
    {
      Console.WriteLine("No operand specified, returning base number");
      return Task.FromResult(analysis);
    }
    var operand = numOperand.NumberValue;

    var operation = request.Settings.Fields[DemoDataTypes.Operation.Key];
    var operationValue = operation.StringValue;

    analysis.Result = operationValue switch
    {
      "Multiply" => (float)(analysis.Result * operand),
      "Divide" => (float)(analysis.Result / operand),
      _ => 0,// you broke it :(
    };

    return Task.FromResult(analysis);
  }

  public override Task<AnalysisParametersResponse> GetAnalysisParameters(Empty request, ServerCallContext context)
  {
    var response = new AnalysisParametersResponse
    {
      ParameterSchema = new AresDataSchema
      {
        Fields =
        {
          [DemoDataTypes.InputNumber.Key] = DemoDataTypes.InputNumber.Value,
          [DemoDataTypes.Operand.Key] = DemoDataTypes.Operand.Value
        }
      }
    };

    return Task.FromResult(response);
  }

  public override Task<AnalyzerCapabilities> GetAnalyzerCapabilities(Empty request, ServerCallContext context)
  {
    var capabilities = new AnalyzerCapabilities
    {
      SettingsSchema = new AresDataSchema
      {
        Fields =
        {
          [DemoDataTypes.Operation.Key] = DemoDataTypes.Operation.Value,
          [DemoDataTypes.RandomTags.Key] = DemoDataTypes.RandomTags.Value,
          [DemoDataTypes.PreselectedTags.Key] = DemoDataTypes.PreselectedTags.Value
        }
      }
    };


    return Task.FromResult(capabilities);
  }

  public override Task<ConnectionStatus> GetConnectionStatus(Empty request, ServerCallContext context)
  {
    var response = new ConnectionStatus { Status = AresStatus.Connected };

    return Task.FromResult(response);
  }

  public override Task<InfoResponse> GetInfo(Empty request, ServerCallContext context)
  {
    var infoResponse = new InfoResponse
    {
      Description = "Give me a number and I'll give it back or multiply it by the multiplier parameter.",
      Name = "DemoAnalyzer",
      Version = "1.0.1"
    };

    return Task.FromResult(infoResponse);
  }

  public override Task<ParameterValidationResult> ValidateInputs(ParameterValidationRequest request, ServerCallContext context)
  {
    Console.WriteLine("Validating inputs");
    if(request.InputSchema.Fields.ContainsKey(DemoDataTypes.InputNumber.Key))
    {
      Console.WriteLine($"Validation found data key {DemoDataTypes.InputNumber.Key}");
    }
    else
    {
      Console.WriteLine($"Did not found data with a key of {DemoDataTypes.InputNumber.Key}.");
      Console.WriteLine("Found following items:");
      foreach(var schemaItem in request.InputSchema.Fields)
      {
        Console.WriteLine($"{schemaItem.Key}:{schemaItem.Value}");
      }
    }

    if(request.InputSchema.Fields.ContainsKey(DemoDataTypes.Operand.Key))
    {
      Console.WriteLine($"Validation found data key {DemoDataTypes.Operand.Key}");
    }
    else
    {
      Console.WriteLine($"Did not found data with a key of {DemoDataTypes.Operand.Key}. But it was optional, so doesn't matter :)");
    }

    // the base input validator will take care of checking for required params so no need to do that manually unless
    // you need some specific validation logic
    return base.ValidateInputs(request, context);
  }
}
