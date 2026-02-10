using Ares.Datamodel;
using Ares.Datamodel.Factories;

namespace DemoRemoteAnalyzer;

public static class DemoDataTypes
{
  public static readonly KeyValuePair<string, SchemaEntry> Operand = new("Operand", AresSchemaBuilder.Entry(AresDataType.Number).AsOptional().Build());
  public static readonly KeyValuePair<string, SchemaEntry> InputNumber = new("InputNumber", AresSchemaBuilder.Entry(AresDataType.Number).Build());
  public static readonly KeyValuePair<string, SchemaEntry> Operation = new(
    "Operation",
    AresSchemaBuilder.Entry(AresDataType.String).WithChoices("Multiply", "Divide").Build());

  public static readonly KeyValuePair<string, SchemaEntry> RandomTags = new("RandomTags",
    AresSchemaBuilder.Entry(AresDataType.StringArray).AsOptional().Build());
  public static readonly KeyValuePair<string, SchemaEntry> PreselectedTags = new("Preselected Tags",
    AresSchemaBuilder.Entry(AresDataType.StringArray).AsOptional().WithChoices("Tag1", "Tag2", "Tag3").Build());
}
