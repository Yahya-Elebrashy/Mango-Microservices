
using Mango.Services.CouponAPI.Shared.Middleware;

namespace Mango.Services.CouponAPI.Shared.Extensions;

public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
