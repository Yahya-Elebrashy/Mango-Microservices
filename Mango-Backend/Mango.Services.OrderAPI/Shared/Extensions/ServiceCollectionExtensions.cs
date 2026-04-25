using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Repositories;
using Mango.Services.OrderAPI.Repositories.IRepository;
using Mango.Services.OrderAPI.Services;
using Mango.Services.OrderAPI.Services.IServices;
using Mango.Services.OrderAPI.UnitOfWork;
using MessageBus;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.OrderAPI.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            return services;
        }

        public static IServiceCollection AddRabbitMQ(this IServiceCollection services)
        {
            services.AddScoped<IMessageBus>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                return new MessageBus.MessageBus(
                    hostname: config["RabbitMQ:Hostname"] ?? "localhost",
                    username: config["RabbitMQ:Username"] ?? "guest",
                    password: config["RabbitMQ:Password"] ?? "guest"
                );
            });

            return services;
        }

        public static IServiceCollection AddAutoMapperConfiguration(this IServiceCollection services)
        {
            IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
            services.AddSingleton(mapper);
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IUnitOfWork, Mango.Services.OrderAPI.UnitOfWork.UnitOfWork>();

            return services;
        }

        public static IServiceCollection AddStripeConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            Stripe.StripeConfiguration.ApiKey = configuration.GetSection("Stripe:SecretKey").Get<string>();

            return services;
        }

        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            return services;
        }

        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("MicroservicePolicy", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
