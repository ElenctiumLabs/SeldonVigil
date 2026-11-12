using MediatR;
using SeldonVigil.Api.Shared.Domain;

namespace SeldonVigil.Api.Shared.Application;

internal interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{ }
