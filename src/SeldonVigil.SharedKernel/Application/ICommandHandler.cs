using MediatR;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.SharedKernel.Application;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result<Unit>>
    where TCommand : ICommand;
