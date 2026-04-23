using System.Net;
using System.Text.Json;

namespace Mango.Services.OrderAPI.Shared.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            KeyNotFoundException  => (HttpStatusCode.NotFound,            exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,  exception.Message),
            ArgumentException     => (HttpStatusCode.BadRequest,          exception.Message),
            InvalidOperationException   => (HttpStatusCode.BadRequest,    exception.Message),
            _                     => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        _logger.LogError(exception,
            "Exception: {Message} | Path: {Path} | StatusCode: {StatusCode}",
            exception.Message,
            context.Request.Path,
            (int)statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var response = new
        {
            IsSuccess  = false,
            Message    = message,
            StatusCode = (int)statusCode
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
