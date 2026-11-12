namespace SeldonVigil.Api.Modules.Security.Domain.Entities;

public enum AutoResponse
{
    SuspendAgent,
    BlockRequest,
    AlertOnly,
    Quarantine,
    AutoRespond
}
