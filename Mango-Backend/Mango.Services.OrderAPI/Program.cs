using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Extensions;
using Mango.Services.OrderAPI.Repositories;
using Mango.Services.OrderAPI.Repositories.IRepository;
using Mango.Services.OrderAPI.Services;
using Mango.Services.OrderAPI.Services.IServices;
using Mango.Services.OrderAPI.Shared.Extensions;
using Mango.Services.OrderAPI.UnitOfWork;
using MessageBus;
using Microsoft.EntityFrameworkCore;
namespace Mango.Services.OrderAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            //Add DB Context => ConnectionStrings
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            builder.Services.AddScoped<IMessageBus>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                return new MessageBus.MessageBus(
                    hostname: config["RabbitMQ:Hostname"] ?? "localhost",
                    username: config["RabbitMQ:Username"] ?? "guest",
                    password: config["RabbitMQ:Password"] ?? "guest"
                );
            });
            IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
            builder.Services.AddSingleton(mapper);
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            builder.Services.AddScoped<IOrderService, OrderService>();
            // Repository & UnitOfWork
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IUnitOfWork, Mango.Services.OrderAPI.UnitOfWork.UnitOfWork>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.AddAppAuthentication();
            builder.Services.AddAuthentication();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MicroservicePolicy", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
            var app = builder.Build();
            app.UseGlobalExceptionHandler();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            Stripe.StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();

            app.UseHttpsRedirection();
            app.UseCors("MicroservicePolicy");
            app.UseAuthorization();


            app.MapControllers();

            ApplyMigration();
            app.Run();

            void ApplyMigration()
            {
                using (var scope = app.Services.CreateScope())
                {
                    var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    if (_db.Database.GetPendingMigrations().Count() > 0)
                    {
                        _db.Database.Migrate();
                    }
                }
            }
        }
    }
}
