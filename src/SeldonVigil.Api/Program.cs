using System.Diagnostics.CodeAnalysis;
using Serilog;
using SeldonVigil.Api.Shared.Infrastructure.Authentication;
using SeldonVigil.Api.Shared.Infrastructure.Configuration;
using SeldonVigil.Api.Shared.Infrastructure.RateLimiting;
using SeldonVigil.Telemetry;

public class Program
{
    [RequiresUnreferencedCode("Calls SeldonVigil.Api.Modules.Security.SecurityModule.AddSeldonVigilSecurityModule(IConfiguration)")]
    [RequiresDynamicCode("Calls Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure<TOptions>(IConfiguration)")]
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Logging
        builder.Host.UseSerilog((ctx, sp, cfg) => cfg
            .ConfigureSeldonVigilLogging(ctx.Configuration, "Api")
            .ReadFrom.Services(sp));

        // Configuration sources
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // User Secrets for local development
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets<Program>(optional: true);
        }

        // Azure KeyVault (optional - set Azure:KeyVaultName to enable)
        var keyVaultName = builder.Configuration["Azure:KeyVaultName"];
        if (!string.IsNullOrEmpty(keyVaultName))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri($"https://{keyVaultName}.vault.azure.net/"),
                new DefaultAzureCredential());
        }

        // Services
        builder.Services.AddSeldonVigilServices(builder.Configuration, builder.Environment);

        var swaggerAvailable = builder.Services.AddSeldonVigilSwagger();

        var app = builder.Build();

        // Validate configuration
        var configValidator = app.Services.GetRequiredService<IConfigurationValidator>();
        var validationResult = configValidator.Validate(app.Configuration, app.Environment.EnvironmentName);
        if (!validationResult.IsValid)
            throw new InvalidOperationException($"Invalid configuration: {validationResult.ErrorMessage}");

        // Block DevAuth in non-dev
        if (!app.Environment.IsDevelopment() && app.Configuration.GetValue("DevAuth:Enabled", false))
            throw new InvalidOperationException("DevAuth:Enabled must not be true in non-development environments.");

        // Database
        if (await app.MigrateOnStartupAsync(args))
            return;

        // Middleware pipeline
        app.UseSeldonVigilSwagger(swaggerAvailable);
        app.UseSeldonVigilRateLimiting();
        app.UseSeldonVigilMiddleware();

        // Endpoints
        app.MapPrometheusScrapingEndpoint("/metrics");
        app.MapSeldonVigilHealthChecks();
        app.MapSeldonVigilEndpoints();

        await app.RunAsync().ConfigureAwait(false);
    }
}
