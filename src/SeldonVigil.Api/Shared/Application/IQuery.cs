using MediatR;
using SeldonVigil.Api.Shared.Domain;

namespace SeldonVigil.Api.Shared.Application;

internal interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
