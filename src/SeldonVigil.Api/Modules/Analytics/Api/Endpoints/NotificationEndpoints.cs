using System.Diagnostics.CodeAnalysis;
using MediatR;
using SeldonVigil.Api.Modules.Analytics.Infrastructure.Persistence;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using Unit = SeldonVigil.SharedKernel.Application.Unit;

namespace SeldonVigil.Api.Modules.Analytics.Api.Endpoints;

internal sealed record NotificationDto(
    string Id,
    string Type,
    string Title,
    string Message,
    string? AgentId,
    string Severity,
    DateTime CreatedAt,
    bool Read);

internal sealed record ListNotificationsQuery(
    bool UnreadOnly = false) : IQuery<IReadOnlyList<NotificationDto>>;

internal sealed class ListNotificationsQueryHandler(
    NotificationRepository repository) : IQueryHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(
        ListNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        var notifications = await repository.GetAllAsync(query.UnreadOnly, cancellationToken).ConfigureAwait(false);
        var dtos = notifications.Select(n => new NotificationDto(
            n.Id.ToString(),
            n.Type,
            n.Title,
            n.Message,
            n.AgentId,
            n.Severity,
            n.CreatedAt,
            n.Read)).ToList();
        return Result.Success<IReadOnlyList<NotificationDto>>(dtos);
    }
}

internal sealed record MarkNotificationAsReadCommand(string Id) : ICommand;

internal sealed class MarkNotificationAsReadCommandHandler(
    NotificationRepository repository) : ICommandHandler<MarkNotificationAsReadCommand>
{
    public async Task<Result<Unit>> Handle(
        MarkNotificationAsReadCommand command,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(command.Id, out var id))
        {
            return Result.Failure<Unit>("Invalid notification ID");
        }

        var notification = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (notification is null)
        {
            return Result.Failure<Unit>("Notification not found");
        }

        notification.MarkAsRead();
        await repository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        return Result.Success(Unit.Value);
    }
}

internal static class NotificationEndpoints
{
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")

            .RequireAuthorization();

        group.MapGet("/", async (ISender mediator, CancellationToken ct) =>
            {
                var query = new ListNotificationsQuery();
                var result = await mediator.Send(query, ct).ConfigureAwait(false);
                return result.Match(Results.Ok, _ => Results.StatusCode(500));
            })
        .WithName("ListNotifications")
        .WithSummary("List all notifications")
        .Produces<IReadOnlyList<NotificationDto>>();

        group.MapGet("/unread", async (ISender mediator, CancellationToken ct) =>
            {
                var query = new ListNotificationsQuery(UnreadOnly: true);
                var result = await mediator.Send(query, ct).ConfigureAwait(false);
                return result.Match(Results.Ok, _ => Results.StatusCode(500));
            })
        .WithName("GetUnreadNotifications")
        .WithSummary("Get unread notifications only")
        .Produces<IReadOnlyList<NotificationDto>>();

        group.MapPatch("/{id}/read", async (string id, ISender mediator, CancellationToken ct) =>
            {
                var command = new MarkNotificationAsReadCommand(id);
                var result = await mediator.Send(command, ct).ConfigureAwait(false);
                return result.Match(_ => Results.Ok(), e => Results.BadRequest(new { error = e }));
            })
        .WithName("MarkNotificationAsRead")
        .WithSummary("Mark a notification as read")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}
