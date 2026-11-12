using SeldonVigil.Api.Modules.Security.Api.Endpoints;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Security.Application.Queries;

internal sealed record GetSecurityDashboardQuery : IQuery<SecurityDashboardDto>;
