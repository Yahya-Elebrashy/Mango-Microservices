using AutoMapper;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Repositories;
using Mango.Services.ShoppingCartAPI.Repositories.IRepository;
using Mango.Services.ShoppingCartAPI.Service;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Mango.Services.ShoppingCartAPI.UnitOfWork;
using MessageBus;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ShoppingCartAPI.Shared.Extensions
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

        public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient("Product", client =>
                client.BaseAddress = new Uri(configuration["ServiceUrls:ProductAPI"]!));

            services.AddHttpClient("Coupon", client =>
                client.BaseAddress = new Uri(configuration["ServiceUrls:CouponAPI"]!));

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Repositories & UnitOfWork
            services.AddScoped<ICartHeaderRepository, CartHeaderRepository>();
            services.AddScoped<ICartDetailsRepository, CartDetailsRepository>();
            services.AddScoped<IUnitOfWork, Mango.Services.ShoppingCartAPI.UnitOfWork.UnitOfWork>();

            // Domain services
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddScoped<ICartService, CartService>();

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
