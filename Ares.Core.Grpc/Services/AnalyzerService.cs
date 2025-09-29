using System;
using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Analyzing;
using Ares.Core.Exceptions;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Connection;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using StateResponse = Ares.Datamodel.Connection.StateResponse;

namespace Ares.Core.Grpc.Services;

public class AnalyzerService(IAnalyzerRepo analyzerRepo, IRemoteAnalyzerManager remoteAnalyzerManager) : AresAnalyzerManagementService.AresAnalyzerManagementServiceBase
{
  private readonly IAnalyzerRepo _analyzerRepo = analyzerRepo;
  private readonly IRemoteAnalyzerManager _remoteAnalyzerManager = remoteAnalyzerManager;

  public override async Task<GetAllAnalyzersResponse> GetAllAnalyzers(Empty request, ServerCallContext context)
  {
    var response = new GetAllAnalyzersResponse();
    var availableAnalyzers = _analyzerRepo.AvailableAnalyzers;
    var infos = await Task.WhenAll(availableAnalyzers.Select(GetInfo));
    response.Analyzers.AddRange(infos);
    return response;
  }

  private async Task<AnalyzerInfo> GetInfo(IAnalyzer analyzer)
  {
    var capabilities = await analyzer.GetCapabilities();

    var info = new AnalyzerInfo
    {
      Name = analyzer.Name,
      Type = analyzer.Type,
      Version = analyzer.Version,
      Description = analyzer.Description,
      UniqueId = analyzer.UniqueId,
      Capabilities = capabilities,
      Url = analyzer is RemoteAnalyzer remoteAnalyzer ? remoteAnalyzer.Address.ToString() : string.Empty
    };

    return info;
  }

  public override async Task<AddRemoteAnalyzerResponse> AddRemoteAnalyzer(
    AddRemoteAnalyzerRequest request,
    ServerCallContext context)
  {
    try
    {
      await _remoteAnalyzerManager.CreateAnalyzer(request.Name, request.Url);
      var response = new AddRemoteAnalyzerResponse
      {
        Success = true
      };
      return response;
    }
    catch(Exception e)
    {
      var response = new AddRemoteAnalyzerResponse
      {
        Success = false,
        ErrorMessage = e.Message
      };
      return response;
    }
  }

  public override async Task<UpdateRemoteAnalyzerResponse> UpdateRemoteAnalyzer(
    UpdateRemoteAnalyzerRequest request,
    ServerCallContext context)
  {
    try
    {
      var analyzerConfig = new AnalyzerConfig { UniqueId = request.AnalyzerId, Name = request.Name, Url = request.Url };
      await _remoteAnalyzerManager.UpdateAnalyzer(analyzerConfig);
      var response = new UpdateRemoteAnalyzerResponse
      {
        Success = true
      };
      return response;
    }
    catch(ItemNotFoundException e)
    {
      var response = new UpdateRemoteAnalyzerResponse
      {
        Success = false,
        ErrorMessage = e.Message
      };
      return response;
    }
  }

  public override async Task<Empty> RemoveRemoteAnalyzer(RemoveRemoteAnalyzerRequest request, ServerCallContext context)
  {
    await _remoteAnalyzerManager.RemoveAnalyzer(request.AnalyzerId);

    return new Empty();
  }

  public override Task<StateResponse> GetState(StateRequest request, ServerCallContext context)
  {
    var response = new StateResponse();
    var analyzer = _analyzerRepo.GetAnalyzerById(request.Id) ?? throw new ItemNotFoundException(request.Id, typeof(IAnalyzer), "Failed to get state as requested analyzer was not found"); ;

    response.State = analyzer.AnalyzerState;
    response.StateMessage = analyzer.StateMessage;

    return Task.FromResult(response);
  }

  public override async Task<AnalyzerInfoResponse> GetInfo(AnalyzerInfoRequest request, ServerCallContext context)
  {
    var analyzer = _analyzerRepo.GetAnalyzerById(request.AnalyzerId);
    if(analyzer is null)
    {
      return new AnalyzerInfoResponse
      {
        Info = new AnalyzerInfo { Name = "Unknown", Description = "Analyzer not found" }
      };
    }
    var info = await GetInfo(analyzer);
    var response = new AnalyzerInfoResponse { Info = info };

    return response;
  }

  public override Task<AresStruct> GetAnalyzerSettings(AnalyzerSettingsRequest request, ServerCallContext context)
  {
    var analyzer = _analyzerRepo.GetAnalyzerById(request.AnalyzerId) ?? throw new ItemNotFoundException(request.AnalyzerId, typeof(IAnalyzer), "Failed to get settings as requested analyzer was not found");
    return Task.FromResult(analyzer.Settings);
  }

  public override async Task<Empty> SetAnalyzerSettings(AnalyzerSettings request, ServerCallContext context)
  {
    var analyzer = _analyzerRepo.GetAnalyzerById(request.AnalyzerId);
    if(analyzer is null)
    {
      return new Empty();
    }

    if(analyzer is RemoteAnalyzer remoteAnalyzer)
    {
      await _remoteAnalyzerManager.UpdateAnalyzerSettings(request);
    }
    else
    {
      analyzer.UpdateSettings(request.Settings);
    }

    return new Empty();
  }
}