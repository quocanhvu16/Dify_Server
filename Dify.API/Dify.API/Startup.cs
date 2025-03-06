using Dify.Common.EntityCore;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Dify.BL.Base;
using Dify.DL.Base;
using Dify.DL.Workflows;
using Dify.BL.Workflows;
using Dify.Common.Database;
using Dify.Common.Cache;
using Dify.Common.Helper;
using Dify.Common.MessageQ;
using Dify.Common.Extensions;
using EasyNetQ;
using Dify.Common.QueueData;
using StackExchange.Redis;

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

            services.AddScoped<IBaseBL, BaseBL>();
            services.AddScoped<IBaseDL, BaseDL>();

            services.AddScoped<IWorkflowBL, WorkflowBL>();
            services.AddScoped<IWorkflowDL, WorkflowDL>();

            services.AddScoped<IDbContext, MySQLDbContext>();
            services.AddScoped<EntityDbContext>();

            var advancedBusSection = Configuration.GetSection("AdvancedBus");
            RabbitMqConfig config = advancedBusSection.Get<RabbitMqConfig>();
            IAdvancedBus advancedBus = RabbitHutch.CreateBus(config.ConnectionString).Advanced;
            services.AddSingleton(advancedBus);

            services.AddRabbitMqPublisher<WorkflowQueueData>(Configuration.GetSection("WorkflowQueue").Get<RabbitMqConfig>(), advancedBus);
        }

        public void ConfigureServicesCommon(IServiceCollection services)
        {
            // Đăng ký các controller trong ứng dụng
            services.AddControllers();

            // Tạo tài liệu API tự động
            services.AddEndpointsApiExplorer();

            // Đăng ký và cấu hình dịch vụ Swagger
            services.AddSwaggerGen(c =>
            {
                // Hiển thị XML comments trong Swagger
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecommerce API", Version = "v1" });
            });

            // Cấu hình Cors
            services.AddCors(p => p.AddPolicy("MyCors", build =>
            {
                build.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
            }));

            MySQLDbContext.RegisterTypeHandler();

            services.AddDbContext<EntityDbContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("MySQLConnection"), ServerVersion.AutoDetect(Configuration.GetConnectionString("MySQLConnection")))
            );

            // Thiết lập ủy quyền
            services.AddAuthorization();

            services.AddHttpContextAccessor();

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = ConfigHelper.GetSetting<string>("Redis:Configuration", "localhost:6379"); // Cấu hình kết nối Redis
                options.InstanceName = ConfigHelper.GetSetting<string>("Redis:InstanceName", "");
            });

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect("localhost:6379"));

            services.AddSingleton<ServiceCached>();
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
