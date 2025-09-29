using Ares.Datamodel;
using CsvHelper.Configuration;

namespace Ares.Core.Device.Remote.State;
public class AresStructDataMap : ClassMap<AresStruct>
{
  public AresStructDataMap()
  {
    Map(s => s.Fields);
  }
}
