using Microsoft.AspNetCore.Mvc;

namespace Savanna.Simulation.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// Summary:
    /// Connects the next request handler and the configured Serilog-backed logger.
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        _next = next;
        _logger = logger;
    }

    /// Summary:
    /// Logs unexpected failures once and returns a safe problem response without internal details.
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {TraceId} was cancelled by the client.", context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted) throw;

            int status = exception is BadHttpRequestException badRequest
                ? badRequest.StatusCode
                : StatusCodes.Status500InternalServerError;
            if (status >= StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Request {TraceId} failed: {Method} {Path}.",
                    context.TraceIdentifier, context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("Rejected request {TraceId} with status {StatusCode}.", context.TraceIdentifier, status);

            context.Response.Clear();
            context.Response.StatusCode = status;
            var problem = new ProblemDetails
            {
                Status = status,
                Title = status >= StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : "Invalid request.",
                Detail = status >= StatusCodes.Status500InternalServerError
                    ? "The request could not be completed. Please try again or start a new game."
                    : "Check the request format and try again.",
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
