namespace SeldonVigil.Api.Shared.Application;

internal interface IPipelineBehavior<in TRequest, TResponse>
    : MediatR.IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{ }
