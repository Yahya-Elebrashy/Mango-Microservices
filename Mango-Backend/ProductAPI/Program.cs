using Logging;
using Mango.Services.ProductAPI.Extensions;
using Mango.Services.ProductAPI.Shared.Extensions;
using Serilog;

namespace ProductAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SerilogConfiguration.Configure("ProductAPI");
            try
            {
                Log.Information("Starting ProductAPI...");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                builder.Services
                    .AddDatabase(builder.Configuration)
                    .AddAutoMapperConfiguration()
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
                Log.Fatal(ex, "ProductAPI failed to start");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}