using Mango.Services.AuthAPI.Shared.Middleware;

namespace Mango.Services.AuthAPI.Shared.Extensions;

public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
