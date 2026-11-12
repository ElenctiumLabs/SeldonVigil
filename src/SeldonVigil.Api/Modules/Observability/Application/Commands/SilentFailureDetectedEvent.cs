using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Observability.Application.Commands;

internal sealed record SilentFailureDetectedEvent(
    TraceId TraceId,
    AgentId AgentId,
    string OperationName,
    DateTime EventTimestamp
) : BaseEvent;
