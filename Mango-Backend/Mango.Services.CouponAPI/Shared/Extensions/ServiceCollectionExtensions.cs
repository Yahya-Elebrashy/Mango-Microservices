using AutoMapper;
using Mango.Services.CouponAPI.Data;
using Mango.Services.CouponAPI.Repositories;
using Mango.Services.CouponAPI.Repositories.IRepository;
using Mango.Services.CouponAPI.Services;
using Mango.Services.CouponAPI.Services.IServices;
using Mango.Services.CouponAPI.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.CouponAPI.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

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
            services.AddScoped<ICouponService, CouponService>();
            services.AddScoped<ICouponRepository, CouponRepository>();
            services.AddScoped<IUnitOfWork, Mango.Services.CouponAPI.UnitOfWork.UnitOfWork>();

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
