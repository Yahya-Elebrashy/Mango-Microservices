using AutoMapper;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Extensions;
using Mango.Services.ShoppingCartAPI.Repositories;
using Mango.Services.ShoppingCartAPI.Repositories.IRepository;
using Mango.Services.ShoppingCartAPI.Service;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Mango.Services.ShoppingCartAPI.Shared.Extensions;
using Mango.Services.ShoppingCartAPI.UnitOfWork;
using MessageBus;
using MessageBus;
using Microsoft.EntityFrameworkCore;
namespace Mango.Services.ShoppingCartAPI
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
            builder.Services.AddHttpClient("Product", u => u.BaseAddress =
            new Uri(builder.Configuration["ServiceUrls:ProductAPI"]));
            builder.Services.AddHttpClient("Coupon", u => u.BaseAddress =
            new Uri(builder.Configuration["ServiceUrls:CouponAPI"]));

            // Repository & UnitOfWork
            builder.Services.AddScoped<ICartHeaderRepository, CartHeaderRepository>();
            builder.Services.AddScoped<ICartDetailsRepository, CartDetailsRepository>();
            builder.Services.AddScoped<IUnitOfWork, Mango.Services.ShoppingCartAPI.UnitOfWork.UnitOfWork>();

            builder.Services.AddControllers();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ICouponService, CouponService>();
            builder.Services.AddScoped<ICartService, CartService>();
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

            app.UseHttpsRedirection();
            app.UseCors("MicroservicePolicy");
            app.UseAuthentication();
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
