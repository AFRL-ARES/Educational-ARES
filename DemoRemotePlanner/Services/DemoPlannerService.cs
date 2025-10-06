using Ares.Datamodel;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Planning.Remote;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DemoRemotePlanner.Services;
public class DemoPlannerService : AresRemotePlannerService.AresRemotePlannerServiceBase
{
  private readonly Random _random;
  private readonly Planner _randomPlanner;
  private readonly Planner _gradualPlanner;

  public DemoPlannerService()
  {
    _random = new Random();

    _randomPlanner = new Planner()
    {
      PlannerName = "Random Planner",
      Description = "A planner that returns random temperatures for the demo device.",
      Version = "1.0.0"
    };

    _gradualPlanner = new Planner()
    {
      PlannerName = "Gradual Planner",
      Description = "A planner that returns a temperature value that gradually increases from the previously provided value by 5 degrees.",
      Version = "1.0.0"
    };
  }

  public override async Task<PlanningResponse> Plan(PlanningRequest request, ServerCallContext context)
  {
    Console.WriteLine("Planning Requested!");
    var inputs = request.PlanningParameters;
    Console.WriteLine($"Received a total of {inputs.Count} parameters to plan for.");
    var response = new PlanningResponse();

    foreach(var parameter in inputs)
    {
      switch(parameter.PlannerName)
      {
        case "Random Planner":
          {
            var plannedParam = await RandomPlanner(parameter);
            response.PlannedParameters.Add(plannedParam);
            break;
          }
        case "Gradual Planner":
          {
            var gradualPlannedParam = await GradualPlanner(parameter);
            response.PlannedParameters.Add(gradualPlannedParam);
            break;
          }
        default:
          {
            Console.WriteLine("Unrecognized Planned Requested! Defaulting to random planner...");
            var plannedParam = await RandomPlanner(parameter);
            response.PlannedParameters.Add(plannedParam);
            break;
          }
      }
    }

    return response;
  }

  public override Task<ConnectionStatus> GetConnectionStatus(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new ConnectionStatus
    {
      Status = AresStatus.Connected,
      Info = "Demo Planner Service is connected!"
    });
  }

  public override Task<InfoResponse> GetInfo(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new InfoResponse
    {
      Name = "Demo Planner Service",
      Description = "A demo of the ARES remote planenr service capabilities",
      Version = "1.0.0"
    });
  }

  public override Task<StateResponse> GetState(Empty request, ServerCallContext context)
  {
    return Task.FromResult(new StateResponse { State = State.Active, StateMessage = "The Demo Planner Service is active!" });
  }

  public override Task<PlannerServiceCapabilities> GetPlannerServiceCapabilities(Empty request, ServerCallContext context)
  {
    var capabilitesResponse = new PlannerServiceCapabilities();

    capabilitesResponse.AvailablePlanners.Add(_randomPlanner);
    capabilitesResponse.AvailablePlanners.Add(_gradualPlanner);
    capabilitesResponse.ServiceName = "Demo Planner Service";
    capabilitesResponse.TimeoutSeconds = 30;
    capabilitesResponse.AcceptedTypes.Add(AresDataType.Number);

    capabilitesResponse.SettingsSchema = new AresDataSchema
    {
      Fields =
      {
        ["Dual Randomization"] = AresSchemaHelper.CreateSchemaEntry(AresDataType.Boolean, true)
      }
    };

    return Task.FromResult(capabilitesResponse);
  }

  public Task<PlannedParameter> RandomPlanner(PlanningParameter aresParameter)
  {
    var randomDouble = _random.NextDouble();
    var plannedParam = new PlannedParameter();
    plannedParam.ParameterName = aresParameter.ParameterName;
    var randomizedValue = (float)(aresParameter.MinimumValue + (randomDouble * (aresParameter.MaximumValue - aresParameter.MinimumValue)));
    plannedParam.ParameterValue = AresValueHelper.CreateNumber(randomizedValue);
    return Task.FromResult(plannedParam);
  }

  public Task<PlannedParameter> GradualPlanner(PlanningParameter aresParameter)
  {
    var response = new PlannedParameter();
    response.ParameterName = aresParameter.ParameterName;

    if(aresParameter.ParameterHistory.Count == 0)
      response.ParameterValue = AresValueHelper.CreateNumber((float)aresParameter.MinimumValue);

    else
    {
      var previousValue = aresParameter.ParameterHistory.Last().PlannedValue;
      double incrementedValue;

      if(previousValue.HasNumberValue)
        incrementedValue = previousValue.NumberValue + 5;

      else
      {
        Console.WriteLine("The previous assigned value didn't exist, setting newest value to minimum allowed value.");
        incrementedValue = aresParameter.MinimumValue;
      }

      if(incrementedValue > aresParameter.MaximumValue)
        response.ParameterValue = AresValueHelper.CreateNumber((float)aresParameter.MaximumValue);

      else
        response.ParameterValue = AresValueHelper.CreateNumber((float)incrementedValue);
    }

    return Task.FromResult(response);
  }
}
