using MediatR;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.SharedKernel.Application;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
