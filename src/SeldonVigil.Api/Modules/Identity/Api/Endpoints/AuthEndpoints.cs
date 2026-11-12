using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.Extensions.Options;
using SeldonVigil.Api.Modules.Identity.Application.Commands;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Identity.Api.Endpoints;

internal sealed record LoginRequest(string Username, string Password);

internal sealed record ApiKeyLoginRequest(string ApiKey);

internal sealed record RefreshTokenRequest(string RefreshToken);

internal sealed record RegisterRequest(string Email, string Name, string Password, string Role);

internal sealed record LoginResponse(string Token, string RefreshToken, string Username, string[] Roles);

internal sealed record DevAuthOptions
{
    public const string SectionName = "DevAuth";
    public string SecretKey { get; init; } = "ThisIsADevelopmentSecretKeyThatShouldBeChangedInProduction!";
    public string Issuer { get; init; } = "seldonvigil-dev";
    public string Audience { get; init; } = "seldonvigil-api";
    public bool Enabled { get; init; } = true;
}

internal static class AuthEndpoints
{
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication")

            .AllowAnonymous();

        // POST /api/v1/auth/login
        group.MapPost("/login", async (
            LoginRequest request,
            ISender mediator,
            IRequestContext requestContext,
            HttpContext http,
            CancellationToken ct) =>
        {
            Console.WriteLine($"Login request received: Username={request.Username}, TenantId={requestContext.TenantId}");
            var result = await mediator.Send(new LoginCommand(
                requestContext.TenantId ?? string.Empty,
                request.Username,
                request.Password,
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString()), ct).ConfigureAwait(false);

            return result.Match(
                success => Results.Ok(new LoginResponse(
                    success.AccessToken,
                    success.RefreshToken,
                    success.Username,
                    success.Roles)),
                error => error.Type switch
                {
                    ErrorType.Validation => Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["credentials"] = [error.Message]
                    }),
                    _ => Results.Problem(error.Message, statusCode: StatusCodes.Status401Unauthorized)
                });
        })
        .WithName("Login")
        .WithSummary("Authenticate with email/password and receive access and refresh tokens")
        .Produces<LoginResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // POST /api/v1/auth/api-key
        group.MapPost("/api-key", async (
            ApiKeyLoginRequest request,
            ISender mediator,
            HttpContext http,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new ApiKeyLoginCommand(
                request.ApiKey,
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString()), ct).ConfigureAwait(false);

            return result.Match(
                success => Results.Ok(new
                {
                    accessToken = success.AccessToken,
                    refreshToken = success.RefreshToken,
                    tokenType = success.TokenType,
                    expiresIn = success.ExpiresInSeconds
                }),
                error => Results.Problem(error.Message, statusCode: StatusCodes.Status401Unauthorized));
        })
        .WithName("ApiKeyLogin")
        .WithSummary("Exchange a service API key for access and refresh tokens")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

#if DEBUG
        // Temporary diagnostics: shows the bound DevAuthOptions + host environment.
        group.MapGet("/auth-debug", (IOptions<DevAuthOptions> devOpts, IHostEnvironment env) => Results.Ok(new
        {
            env = env.EnvironmentName,
            enabled = devOpts.Value.Enabled,
            issuer = devOpts.Value.Issuer,
            audience = devOpts.Value.Audience,
            secretConfigured = !string.IsNullOrWhiteSpace(devOpts.Value.SecretKey)
        })).AllowAnonymous();
#endif

        // POST /api/v1/auth/refresh
        group.MapPost("/refresh", async (
            RefreshTokenRequest request,
            ISender mediator,
            IRequestContext requestContext,
            HttpContext http,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new RefreshTokenCommand(
                requestContext.TenantId ?? string.Empty,
                request.RefreshToken,
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString()), ct).ConfigureAwait(false);

            return result.Match(
                success => Results.Ok(new LoginResponse(
                    success.AccessToken,
                    success.RefreshToken,
                    success.Username,
                    success.Roles)),
                error => Results.Problem(error.Message, statusCode: StatusCodes.Status401Unauthorized));
        })
        .WithName("RefreshToken")
        .WithSummary("Exchange a refresh token for a new access token")
        .Produces<LoginResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // POST /api/v1/auth/logout
        group.MapPost("/logout", async (
            RefreshTokenRequest request,
            ISender mediator,
            IRequestContext requestContext,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new LogoutCommand(
                request.RefreshToken,
                requestContext.UserId), ct).ConfigureAwait(false);

            return result.Match(
                _ => Results.NoContent(),
                error => Results.Problem(error.Message, statusCode: StatusCodes.Status400BadRequest));
        })
        .WithName("Logout")
        .WithSummary("Revoke the current refresh token")
        .Produces(StatusCodes.Status204NoContent);

        // POST /api/v1/auth/register
        group.MapPost("/register", async (
            RegisterRequest request,
            ISender mediator,
            IRequestContext requestContext,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            // Registration is opt-in; disabled by default to prevent open sign-ups.
            if (!configuration.GetValue("Identity:AllowRegistration", false))
            {
                return Results.NotFound();
            }

            var result = await mediator.Send(new RegisterUserCommand(
                requestContext.TenantId ?? string.Empty,
                request.Email,
                request.Name,
                request.Password,
                request.Role), ct).ConfigureAwait(false);

            return result.Match(
                id => Results.Created($"/api/v1/users/{id}", new { Id = id }),
                error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(error.Message),
                    ErrorType.Validation => Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["user"] = [error.Message]
                    }),
                    _ => Results.StatusCode(500)
                });
        })
        .WithName("Register")
        .WithSummary("Register a new user in the current tenant")
        .Produces(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        return app;
    }
}
