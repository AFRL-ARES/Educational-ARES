using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Extensions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace Ares.Core.Analyzing;

public class RemoteAnalyzer : AnalyzerBase
{
  private readonly GrpcChannel _channel;
  private AnalyzerCapabilities _capabilities = new();
  private AresDataSchema _parameters = new();

  public RemoteAnalyzer(string name, Uri address, string id) : base(name, "", "_._._", id)
  {
    _channel = GrpcChannel.ForAddress(address);
    Address = address;
  }

  public RemoteAnalyzer(string name, Uri address) : base(name, "", "_._._")
  {
    _channel = GrpcChannel.ForAddress(address);
    Address = address;
  }

  public Uri Address { get; }

  public override async Task<Analysis> Analyze(AresStruct inputs, RequestMetadata metadata, CancellationToken cancellationToken = default)
  {
    var client = GetClient();
    var analysisRequest = new AnalysisRequest
    {
      Inputs = inputs,
      Settings = Settings
    };
    var analysis = await client.AnalyzeAsync(analysisRequest, cancellationToken: cancellationToken);

    return analysis;
  }


  /// <summary>
  /// Gives the option of settings override.
  /// </summary>
  /// <param name="inputs"></param>
  /// <param name="settings">These will add-to/override existing settings values if provided. </param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public override Task<Analysis> Analyze(AresStruct inputs, AresStruct settings, RequestMetadata metadata, CancellationToken cancellationToken = default)
  {
    var client = GetClient();
    var mergedSettings = Settings.AppendStruct(settings);
    var analysisRequest = new AnalysisRequest
    {
      Inputs = inputs,
      Settings = mergedSettings
    };
    return client.AnalyzeAsync(analysisRequest, cancellationToken: cancellationToken).ResponseAsync;
  }

  public override async Task<AnalyzerCapabilities> GetCapabilities(CancellationToken cancellationToken = default)
  {
    var client = GetClient();
    try
    {
      var capabilities = await client.GetAnalyzerCapabilitiesAsync(new Empty(), cancellationToken: cancellationToken);
      _capabilities = capabilities;
      return capabilities;
    }
    catch(RpcException)
    {
      return _capabilities;
    }
  }

  public override async Task<AresDataSchema> GetParameters(CancellationToken cancellationToken)
  {
    var client = GetClient();
    try
    {
      var response = await client.GetAnalysisParametersAsync(new Empty(), cancellationToken: cancellationToken);
      _parameters = response.ParameterSchema;
      return response.ParameterSchema;
    }
    catch(RpcException)
    {
      return _parameters;
    }
  }

  public override async Task Init()
  {
    await UpdateInfo();
    await UpdateState();
    await UpdateParameters();
    await UpdateCapabilities();
  }

  internal async Task UpdateInfo()
  {
    var client = GetClient();
    try
    {
      var info = await client.GetInfoAsync(new Empty());
      Type = info.Name;
      Version = info.Version;
      Description = info.Description;
    }
    catch(RpcException)
    {
    }
  }

  internal async Task UpdateState()
  {
    var client = GetClient();
    try
    {
      var state = await client.GetStateAsync(new Empty());
      AnalyzerState = state.State;
      StateMessage = state.StateMessage;
    }
    catch(RpcException e)
    {
      AnalyzerState = State.Inactive;
      StateMessage = $"Failed to connect to analyzer: {e.Message}";
    }
  }

  internal async Task UpdateCapabilities()
  {
    var client = GetClient();
    try
    {
      _capabilities = await client.GetAnalyzerCapabilitiesAsync(new Empty());
    }
    catch(RpcException)
    {
    }

    if(_capabilities.TimeoutSeconds > 0)
    {
      AnalysisTimeout = TimeSpan.FromSeconds(_capabilities.TimeoutSeconds);
    }
    else
    {
      AnalysisTimeout = TimeSpan.MaxValue;
    }

    var newSettings = _capabilities.SettingsSchema?.Fields.Where(entry => !Settings.Fields.ContainsKey(entry.Key)) ?? [];
    var removedSettings = Settings.Fields.Where(entry => !_capabilities.SettingsSchema?.Fields.ContainsKey(entry.Key) ?? false);

    foreach(var removedSetting in removedSettings)
    {
      Settings.Fields.Remove(removedSetting.Key);
    }

    foreach(var newSetting in newSettings)
    {
      if(newSetting.Value.Type == AresDataType.String)
      {
        Settings.Fields[newSetting.Key] = AresValueHelper.CreateDefault(newSetting.Value.Type, newSetting.Value.StringChoices?.Strings);
      }
      else if(newSetting.Value.Type == AresDataType.Number)
      {
        Settings.Fields[newSetting.Key] = AresValueHelper.CreateDefault(
          newSetting.Value.Type,
          newSetting.Value.NumberChoices?.Numbers);
      }
      else
      {
        Settings.Fields[newSetting.Key] = AresValueHelper.CreateDefault(newSetting.Value.Type);
      }
    }
  }

  internal async Task UpdateParameters()
  {
    var client = GetClient();
    try
    {
      var response = await client.GetAnalysisParametersAsync(new Empty());
      _parameters = response.ParameterSchema;
    }
    catch(RpcException)
    {
    }
  }

  internal async Task UpdateInfo(AnalyzerInfo info)
  {
    Type = info.Type;
    Description = info.Description;
    Version = info.Version;
    _capabilities = info.Capabilities;
    await UpdateCapabilities();
  }

  private AresRemoteAnalyzerService.AresRemoteAnalyzerServiceClient GetClient()
  {
    return new AresRemoteAnalyzerService.AresRemoteAnalyzerServiceClient(_channel);
  }
}
