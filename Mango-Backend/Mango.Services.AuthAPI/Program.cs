using Logging;
using Mango.Services.AuthAPI.Shared.Extensions;
using Serilog;

namespace Mango.Services.AuthAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SerilogConfiguration.Configure("AuthAPI");
            try
            {
                Log.Information("Starting AuthAPI...");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                builder.Services
                    .AddDatabase(builder.Configuration)
                    .AddIdentityConfiguration()
                    .AddApplicationServices(builder.Configuration)
                    .AddSwaggerConfiguration()
                    .AddCorsConfiguration()
                    .AddControllers();

                var app = builder.Build();

                app.UseSwaggerConfiguration()
                   .UsePipelineConfiguration()
                   .ApplyMigrations();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "AuthAPI failed to start");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}