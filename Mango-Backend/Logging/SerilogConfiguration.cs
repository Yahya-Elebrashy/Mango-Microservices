using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logging
{
    public static class SerilogConfiguration
    {
        public static void Configure(string serviceName)
        {
            Log.Logger = new LoggerConfiguration()

                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)

                .Enrich.WithProperty("Service", serviceName)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()


                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")

                .WriteTo.File(
                    path: $"logs/{serviceName}-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")

                .CreateLogger();
        }
    }
}
