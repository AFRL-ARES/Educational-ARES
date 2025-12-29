using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Templates;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using NuGet.Packaging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Backend.ViewModels.Automation.CampaignEdit;

public partial class AnalyzerDesignerViewModel : ReactiveObject
{
  private readonly AresAnalysisService.AresAnalysisServiceClient _analysisServiceClient;
  private readonly ExperimentTemplate _experimentTemplate;
  readonly IEnumerable<CommandDesignerViewModel> _commandDesignerViewModels;
  readonly AresAnalyzerManagementService.AresAnalyzerManagementServiceClient _analyzerManagementClient;
  private string? _analyzerId = null;

  public AnalyzerDesignerViewModel(AresAnalysisService.AresAnalysisServiceClient analysisServiceClient,
    AresAnalyzerManagementService.AresAnalyzerManagementServiceClient analyzerManagementClient, ExperimentTemplate experimentTemplate, IEnumerable<CommandDesignerViewModel> commandDesignerViewModels)
  {
    _analyzerManagementClient = analyzerManagementClient;
    _commandDesignerViewModels = commandDesignerViewModels;
    _analysisServiceClient = analysisServiceClient;
    _experimentTemplate = experimentTemplate;

    AnalyzerId = string.IsNullOrEmpty(_experimentTemplate.AnalyzerId) ? "NONE-ANALYZER" : _experimentTemplate.AnalyzerId;
    OutputInputMappings = [];
  }

  [Reactive]
  public partial ExperimentOutputAnalyzerInputMapping[] OutputInputMappings { get; private set; }

  public async Task UpdateMappings()
  {
    if(AnalyzerId == default)
    {
      OutputInputMappings = [];
      return;
    }

    var parameters = await _analysisServiceClient.GetAnalyzerParametersAsync(
      new AnalyzerParametersRequest { AnalyzerId = AnalyzerId });

    var inputMappings = parameters.AnalysisSchema.Fields.Select(field => new ExperimentOutputAnalyzerInputMapping(field.Key, field.Value.Type, !field.Value.Optional)).ToArray();

    CalculateAppropriateOutputs(inputMappings);

    OutputInputMappings = inputMappings;

    foreach(var analyzerMapping in _experimentTemplate.AnalyzerMaps)
    {
      var inputMapping = OutputInputMappings.FirstOrDefault(mapping => mapping.AnalyzerInputKey == analyzerMapping.Key);
      if(inputMapping is null)
      {
        continue;
      }

      inputMapping.ExperimentOutput = analyzerMapping.Value;
    }
  }

  public async Task UpdateAvailableAnalyzers()
  {
    var analyzers = await _analyzerManagementClient.GetAllAnalyzersAsync(new Empty());
    AvailableAnalyzers = analyzers.Analyzers.ToList();
  }

  public async Task CheckAnalyzer()
  {
    if(string.IsNullOrEmpty(AnalyzerId))
      return;

    if(AvailableAnalyzers is null)
      await UpdateAvailableAnalyzers();
    
    var request = new AnalyzerInfoRequest() { AnalyzerId = AnalyzerId };
    Analyzer = _analyzerManagementClient.GetInfo(request).Info;
  }

  private void CalculateAppropriateOutputs(IEnumerable<ExperimentOutputAnalyzerInputMapping> outputInputMappings)
  {
    foreach(var outputInputMap in outputInputMappings)
    {
      var outputs = _commandDesignerViewModels.SelectMany(cdv => cdv.OutputKeyMap)
        .Where(okm => okm.DeviceOutputType == outputInputMap.InputType)
        .Select(okm => okm.CustomName)
        .ToArray();

      outputInputMap.MatchingOutputs = outputs;
    }
  }

  public void Save()
  {
    var analyzerMappings = OutputInputMappings
      .Where(mapping => mapping.ExperimentOutput is not null)
      .Select(mapping => new KeyValuePair<string, string>(mapping.AnalyzerInputKey, mapping.ExperimentOutput!));

    _experimentTemplate.AnalyzerMaps.Clear();
    _experimentTemplate.AnalyzerMaps.AddRange(analyzerMappings);
    _experimentTemplate.AnalyzerId = AnalyzerId;
  }

  public string? AnalyzerId
  {
    get => _analyzerId;

    set
    {
      this.RaiseAndSetIfChanged(ref _analyzerId, value);
      _ = CheckAnalyzer();
      _ = UpdateMappings();
    }
  }

  public AnalyzerInfo? Analyzer { get; private set; }

  public IEnumerable<AnalyzerInfo>? AvailableAnalyzers { get; set; }
}

public record ExperimentOutputAnalyzerInputMapping
{
  public ExperimentOutputAnalyzerInputMapping(string analyzerInputKey, AresDataType inputDataType, bool required)
  {
    AnalyzerInputKey = analyzerInputKey;
    InputType = inputDataType;
    Required = required;
  }

  public string AnalyzerInputKey { get; }
  public AresDataType InputType { get; }
  public bool Required { get; }
  public string[] MatchingOutputs { get; set; } = [];
  public string? ExperimentOutput { get; set; }
}
