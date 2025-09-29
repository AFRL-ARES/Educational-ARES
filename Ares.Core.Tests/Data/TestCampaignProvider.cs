using Ares.Core.Analyzing;
using Ares.Core.Tests.Data.Device;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Ares.Test;

namespace Ares.Core.Tests.Data;

internal class TestCampaignProvider
{
  public static CampaignTemplate GetSampleCampaignTemplate(IAnalyzer analyzer)
  {
    var device = new TestDevice();
    var commandTemplate1 = GetCommandTemplate(0, GetCommandMetadata(TestDeviceCommand.Record.ToString(), device.UniqueId, GetOutputMetadata(typeof(TestReply).FullName)), GetParameter(TestDeviceCommandParameter.ReplyParameter.ToString(), "10", 0));
    var commandTemplate2 = GetCommandTemplate(1, GetCommandMetadata(TestDeviceCommand.Record.ToString(), device.UniqueId, GetOutputMetadata(typeof(TestReply).FullName)), GetParameter(TestDeviceCommandParameter.ReplyParameter.ToString(), "20", 0));
    var commandTemplate3 = GetCommandTemplate(2, GetCommandMetadata(TestDeviceCommand.Record.ToString(), device.UniqueId, GetOutputMetadata(typeof(TestReply).FullName)), GetParameter(TestDeviceCommandParameter.ReplyParameter.ToString(), "30", 0));
    var commandTemplate4 = GetCommandTemplate(3, GetCommandMetadata(TestDeviceCommand.Record.ToString(), device.UniqueId, GetOutputMetadata(typeof(TestReply).FullName)), GetParameter(TestDeviceCommandParameter.ReplyParameter.ToString(), "40", 0));

    var stepTemplate = GetStepTemplate("Test Step", false, commandTemplate1, commandTemplate2, commandTemplate3, commandTemplate4);

    var experimentTemplate = GetExperimentTemplate(analyzer, "Test Experiment", commandTemplate4.UniqueId, stepTemplate);

    var campaignTemplate = GetCampaignTemplate("Test Campaign", experimentTemplate);

    return campaignTemplate;
  }

  public static CampaignTemplate GetCampaignTemplate(string name, ExperimentTemplate experimentTemplate)
  {
    var campaignTemplate = new CampaignTemplate
    {
      Name = name,
      UniqueId = Guid.NewGuid().ToString()
    };

    campaignTemplate.ExperimentTemplate = experimentTemplate;

    return campaignTemplate;
  }

  public static Parameter GetParameter(string name, string value, int idx)
  {
    var parameter = new Parameter();
    parameter.Index = idx;
    parameter.Planned = false;
    parameter.UniqueId = Guid.NewGuid().ToString();
    parameter.Value = AresValueHelper.CreateString(value);

    parameter.Metadata = new ParameterMetadata
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = name,
      Unit = "Test"
    };

    return parameter;
  }

  public static ExperimentTemplate GetExperimentTemplate(string analyzerId,
    string name,
    string outputCommand,
    params StepTemplate[] stepTemplates)
  {
    var experimentTemplate = new ExperimentTemplate
    {
      AnalyzerId = analyzerId,
      Name = name,
      Resolved = true,
      UniqueId = Guid.NewGuid().ToString(),
    };

    experimentTemplate.StepTemplates.AddRange(stepTemplates);

    experimentTemplate.AnalyzerMaps["TestAnalyzerInput"] = "TestExperimentOutput";

    return experimentTemplate;
  }

  public static ExperimentTemplate GetExperimentTemplate(IAnalyzer analyzer,
    string name,
    string outputCommand,
    params StepTemplate[] stepTemplates)
  {
    return GetExperimentTemplate(analyzer.UniqueId, name, outputCommand, stepTemplates);
  }

  public static CommandTemplate GetCommandTemplate(int idx, CommandMetadata metadata, params Parameter[] parameters)
  {
    var template = new CommandTemplate
    {
      Index = idx,
      Metadata = metadata
    };

    template.Parameters.AddRange(parameters);
    template.UniqueId = metadata.UniqueId;

    template.UserOutputKeyMap["TestDeviceOutput"] = "TestExperimentOutput";

    return template;
  }

  public static StepTemplate GetStepTemplate(string name, bool parallel, params CommandTemplate[] commandTemplates)
  {
    var stepTemplate = new StepTemplate
    {
      Name = name,
      UniqueId = Guid.NewGuid().ToString()
    };

    stepTemplate.CommandTemplates.AddRange(commandTemplates);

    return stepTemplate;
  }

  public static CommandMetadata GetCommandMetadata(string cmdName, string deviceId, OutputMetadata outputMeta)
    => new()
    {
      DeviceId = deviceId,
      Name = cmdName,
      UniqueId = Guid.NewGuid().ToString(),
      OutputMetadata = outputMeta
    };

  public static OutputMetadata GetOutputMetadata(string typeName, int idx = 0)
    => new()
    {
      DataSchema = AresSchemaHelper.CreateSchema("testCampaignProvider", AresDataType.Number),
      Index = idx,
      UniqueId = Guid.NewGuid().ToString()
    };
}
