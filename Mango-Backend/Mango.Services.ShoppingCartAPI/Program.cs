using Logging;
using Mango.Services.ShoppingCartAPI.Extensions;
using Mango.Services.ShoppingCartAPI.Shared.Extensions;
using Serilog;

namespace Mango.Services.ShoppingCartAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SerilogConfiguration.Configure("ShoppingCartAPI");
            try
            {
                Log.Information("Starting ShoppingCartAPI...");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                builder.Services
                    .AddDatabase(builder.Configuration)
                    .AddRabbitMQ()
                    .AddAutoMapperConfiguration()
                    .AddHttpClients(builder.Configuration)
                    .AddApplicationServices()
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
                Log.Fatal(ex, "ShoppingCartAPI failed to start");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}