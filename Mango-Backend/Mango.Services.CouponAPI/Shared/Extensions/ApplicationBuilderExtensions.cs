using Mango.Services.CouponAPI.Data;
using Microsoft.EntityFrameworkCore;
namespace Mango.Services.CouponAPI.Shared.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static WebApplication UseSwaggerConfiguration(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            return app;
        }

        public static WebApplication UsePipelineConfiguration(this WebApplication app)
        {
            app.UseGlobalExceptionHandler();
            app.UseHttpsRedirection();
            app.UseCors("MicroservicePolicy");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            return app;
        }

        public static WebApplication ApplyMigrations(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (db.Database.GetPendingMigrations().Any())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
                logger.LogInformation("Applying {Count} pending migration(s)", db.Database.GetPendingMigrations().Count());
                db.Database.Migrate();
                logger.LogInformation("Migrations applied successfully");
            }

            return app;
        }
    }
}
