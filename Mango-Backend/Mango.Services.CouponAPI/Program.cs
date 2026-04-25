using Logging;
using Mango.Services.CouponAPI.Extensions;
using Mango.Services.CouponAPI.Shared.Extensions;
using Serilog;

namespace Mango.Services.CouponAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SerilogConfiguration.Configure("CouponAPI");
            try
            {
                Log.Information("Starting CouponAPI...");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                builder.Services
                    .AddDatabase(builder.Configuration)
                    .AddAutoMapperConfiguration()
                    .AddApplicationServices()
                    .AddStripeConfiguration(builder.Configuration)
                    .AddSwaggerConfiguration()
                    .AddCorsConfiguration()
                    .AddControllers();

                builder.AddAppAuthentication();
                builder.Services.AddAuthentication();

                var app = builder.Build();

                app.UseSwaggerConfiguration()
                   .UsePipelineConfiguration()
                   .ApplyMigrations();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "CouponAPI failed to start");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}