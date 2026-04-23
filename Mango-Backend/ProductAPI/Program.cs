using AutoMapper;
using Mango.Services.ProductAPI;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Extensions;
using Mango.Services.ProductAPI.Repositories;
using Mango.Services.ProductAPI.Repositories.IRepository;
using Mango.Services.ProductAPI.Services;
using Mango.Services.ProductAPI.Services.IServices;
using Mango.Services.ProductAPI.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ProductAPI
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
			IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
			builder.Services.AddSingleton(mapper);
			builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            builder.Services.AddScoped<IProductService, ProductService>();
            // Repository & UnitOfWork
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IProductService, ProductService>();
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
			app.UseStaticFiles();

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
