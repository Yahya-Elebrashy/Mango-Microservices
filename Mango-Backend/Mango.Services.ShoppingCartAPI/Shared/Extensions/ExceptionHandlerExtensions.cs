
using Mango.Services.ShoppingCartAPI.Shared.Middleware;

namespace Mango.Services.ShoppingCartAPI.Shared.Extensions;

public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
