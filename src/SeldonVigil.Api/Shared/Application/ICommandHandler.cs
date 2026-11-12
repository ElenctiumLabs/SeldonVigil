using MediatR;
using SeldonVigil.Api.Shared.Domain;
using Unit = SeldonVigil.SharedKernel.Application.Unit;

namespace SeldonVigil.Api.Shared.Application;

internal interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{ }

internal interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result<Unit>>
    where TCommand : ICommand
{ }
