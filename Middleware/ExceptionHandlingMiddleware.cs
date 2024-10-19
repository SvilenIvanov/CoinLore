namespace CoinLore.Middleware;

using Exceptions;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (HttpStatusCodeException ex)
        {
            _logger.LogError($"Error: {ex.Message}, Status Code: {ex.StatusCode}");
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(new
            {
                StatusCode = ex.StatusCode,
                Error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"An unexpected error occurred: {ex.Message}");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Error = "An internal server error occurred."
            });
        }
    }
}