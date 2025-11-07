using Ares.Core.Analyzing;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Ares.Core.Tests;

internal class AnalyzerManagerTests
{
  private IAnalyzerRepo _analyzerRepo;


  [SetUp]
  public void SetUp()
  {
    var dbCtxFactory = new Mock<IDbContextFactory<CoreDatabaseContext>>();
    _analyzerRepo = new AnalyzerRepo();
  }

  private class TempAnalyzer : AnalyzerBase
  {
    public TempAnalyzer(string name, string version) : base(name, "TempAnalyzer", version)
    {
    }

    public override Task<Analysis> Analyze(AresStruct inputs, RequestMetadata metadata, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override Task<Analysis> Analyze(AresStruct inputs, AresStruct settings, RequestMetadata metadata, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override Task<AnalyzerCapabilities> GetCapabilities(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override Task<AresDataSchema> GetParameters(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }
}
