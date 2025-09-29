using Ares.Core.Analyzing;
using Ares.Core.Execution.Extensions;
using Ares.Datamodel;
using Ares.Datamodel.Connection;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace Ares.Core.Validation.Validators;

public static class GoodAnalyzerValidator
{
  public static async Task<ValidationResult> Validate(ExperimentTemplate experimentTemplate, IAnalyzerRepo analyzerRepo)
  {
    if(experimentTemplate.AnalyzerId is null)
      return new ValidationResult(true);

    var analyzer = analyzerRepo.GetAnalyzerById(experimentTemplate.AnalyzerId);

    if(analyzer is null)
      return new ValidationResult(false, $"Unable to find analyzer with id of {experimentTemplate.AnalyzerId}");

    if(analyzer.AnalyzerState != State.Active)
    {
      return new ValidationResult(false, $"Unable to use analyzer {analyzer.Name} as it is is not currently active.\n{analyzer.StateMessage}");
    }

    var outputCommands = experimentTemplate.GetAllOutputCommands();

    var analysisParameterSchema = await analyzer.GetParameters();
    var requiredAnalysisInputs = analysisParameterSchema.Fields.Where(input => !input.Value.Optional).ToArray();
    if(!outputCommands.Any())
    {
      if(!requiredAnalysisInputs.Any())
        return new ValidationResult(true);
      else
        return new ValidationResult(false, $"Experiment does not have any output commands set, but has analyzer {analyzer.Name} assigned");
    }

    var inputSchema = new AresDataSchema();

    foreach(var map in experimentTemplate.AnalyzerMaps)
    {
      var matchingCommand = outputCommands.FirstOrDefault(cmd => cmd.UserOutputKeyMap.Values.Contains(map.Value));

      if(matchingCommand is null)
        continue;

      var matchingMap = matchingCommand.UserOutputKeyMap.FirstOrDefault(userMap => userMap.Value == map.Value);
      var outputSchemaEntry = matchingCommand.Metadata.OutputMetadata.DataSchema.Fields.FirstOrDefault(field => field.Key == matchingMap.Key);
      inputSchema.AddEntry(map.Key, outputSchemaEntry.Value.Type);
    }

    var result = await analyzer.ValidateInputs(inputSchema);
    var validationResult = new ValidationResult(result.Success, result.Messages);

    return validationResult;
  }

  public static async Task<ValidationResult> Validate(IEnumerable<ExperimentTemplate> experimentTemplates, IAnalyzerRepo analyzerManager)
  {
    var validationTasks = experimentTemplates.Select(template => Validate(template, analyzerManager)).ToArray();
    var validations = await Task.WhenAll(validationTasks);
    return new ValidationResult(validations);
  }
}
