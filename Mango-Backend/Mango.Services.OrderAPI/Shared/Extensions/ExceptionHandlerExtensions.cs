
using Mango.Services.OrderAPI.Shared.Middleware;

namespace Mango.Services.OrderAPI.Shared.Extensions;

public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
