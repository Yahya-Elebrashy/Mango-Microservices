using Logging;
using Mango.Services.EmailAPI.Extensions;
using Serilog;


namespace Mango.Services.EmailAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SerilogConfiguration.Configure("EmailAPI");
            try
            {
                Log.Information("Starting EmailAPI...");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                builder.Services
                    .AddDatabase(builder.Configuration)
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
                Log.Fatal(ex, "EmailAPI failed to start");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}