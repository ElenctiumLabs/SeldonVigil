using MediatR;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.SharedKernel.Application;

public interface ICommand : IRequest<Result<Unit>>;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
