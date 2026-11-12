using Asp.Versioning.ApiExplorer;

namespace SeldonVigil.Api.Shared.Infrastructure.Configuration;

public static class ConfigureSwagger
{
    public static bool AddSeldonVigilSwagger(this IServiceCollection services)
    {
        var swaggerAvailable = true;
        try
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
            });
        }
        catch (TypeLoadException tle)
        {
            Console.WriteLine($"[WARN] Swagger registration failed: {tle.Message}");
            swaggerAvailable = false;
        }
        catch (MissingMethodException mme)
        {
            Console.WriteLine($"[WARN] Swagger registration failed: {mme.Message}");
            swaggerAvailable = false;
        }

        return swaggerAvailable;
    }

    public static void UseSeldonVigilSwagger(this WebApplication app, bool swaggerAvailable)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment() && swaggerAvailable)
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "swagger/{documentName}/swagger.json";
            });
            app.UseSwaggerUI(options =>
            {
                var apiVersionDescriptionProvider = app.Services
                    .GetRequiredService<IEnumerable<IApiVersionDescriptionProvider>>()
                    .FirstOrDefault();

                if (apiVersionDescriptionProvider != null)
                {
                    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
                            $"SeldonVigil API {description.ApiVersion}");
                    }
                }
                else
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SeldonVigil API v1");
                }
            });
        }
        else if (app.Environment.IsDevelopment() && !swaggerAvailable)
        {
            Console.WriteLine("[WARN] Swagger is not available due to assembly version mismatch. Skipping Swagger UI.");
        }
    }
}
