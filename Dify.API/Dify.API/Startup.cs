using Dify.BL.Console;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Text;

namespace Dify.Console.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureServicesCommon(services);

            services.AddScoped<IConsoleBL, ConsoleBL>();
        }

        public static void ConfigureServicesCommon(IServiceCollection services)
        {
            // Đăng ký các controller trong ứng dụng
            services.AddControllers();

            // Tạo tài liệu API tự động
            services.AddEndpointsApiExplorer();

            // Đăng ký và cấu hình dịch vụ Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecommerce API", Version = "v1" });
            });

            // Cấu hình Cors
            services.AddCors(p => p.AddPolicy("MyCors", build =>
            {
                build.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
            }));

            // Thiết lập ủy quyền
            services.AddAuthorization();

            services.AddHttpContextAccessor();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Configure the HTTP request pipeline.
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseCors("MyCors");

            app.UseRouting();

            app.UseAuthentication();

            //app.UseMiddleware<ClaimValueMiddleware>();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseStaticFiles();
        }
    }
}
