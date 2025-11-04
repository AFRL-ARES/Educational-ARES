using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Planning;

public record PlanResult(ParameterMetadata Metadata, AresValue Value);

public record PlanResponse(IList<PlanResult> Results, Outcome Outcome, string ErrorString);