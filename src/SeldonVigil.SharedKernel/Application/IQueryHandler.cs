using MediatR;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.SharedKernel.Application;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
