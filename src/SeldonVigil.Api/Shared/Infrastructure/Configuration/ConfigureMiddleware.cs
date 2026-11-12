using SeldonVigil.Api.Shared.Infrastructure.Extensions;

namespace SeldonVigil.Api.Shared.Infrastructure.Configuration;

public static class ConfigureMiddleware
{
    public static void UseSeldonVigilMiddleware(this WebApplication app)
    {
        app.UseExceptionHandler(options =>
        {
            options.Run(async context =>
            {
                var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                if (exception != null)
                {
                    Console.WriteLine($"[ERROR] {context.Request.Path}: {exception.Message}");
                    Console.WriteLine($"[ERROR] Stack trace: {exception.StackTrace}");
                    if (exception.InnerException != null)
                    {
                        Console.WriteLine($"[ERROR] Inner exception: {exception.InnerException.Message}");
                        Console.WriteLine($"[ERROR] Inner stack trace: {exception.InnerException.StackTrace}");
                    }
                }
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = "An error occurred", message = exception?.Message }).ConfigureAwait(false);
            });
        });

        app.UseHttpsRedirection();
        app.UseCors("SeldonVigilCors");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRequestContext();
        app.UseTenantResolution();
    }
}
