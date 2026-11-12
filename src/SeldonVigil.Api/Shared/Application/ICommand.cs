using MediatR;
using SeldonVigil.Api.Shared.Domain;
using Unit = SeldonVigil.SharedKernel.Application.Unit;

namespace SeldonVigil.Api.Shared.Application;

internal interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
internal interface ICommand : IRequest<Result<Unit>> { }
