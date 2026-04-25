using Logging;
using Mango.Services.OrderAPI.Extensions;
using Mango.Services.OrderAPI.Shared.Extensions;
using Serilog;

namespace Mango.Services.OrderAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SerilogConfiguration.Configure("OrderAPI");
            try
            {
                Log.Information("Starting OrderAPI...");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                builder.Services
                    .AddDatabase(builder.Configuration)
                    .AddRabbitMQ()
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
                Log.Fatal(ex, "OrderAPI failed to start");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}