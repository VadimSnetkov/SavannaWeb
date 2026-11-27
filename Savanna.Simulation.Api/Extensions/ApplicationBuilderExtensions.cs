using Savanna.Simulation.Api.Middleware;

namespace Savanna.Simulation.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    /// Summary:
    /// Installs the custom exception middleware before the API endpoints.
    public static IApplicationBuilder UseSavannaExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
