using System.Reflection;

namespace Dify.Console.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder
                            .ConfigureKestrel(serverOptions => { })
                            .UseIISIntegration()
                            .UseContentRoot(Directory.GetCurrentDirectory())
                            .ConfigureAppConfiguration((hostingContext, config) =>
                            {
                                var env = hostingContext.HostingEnvironment;
                                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                                if (env.IsDevelopment())
                                {
                                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                                    if (appAssembly != null)
                                    {
                                        config.AddUserSecrets(appAssembly, true);
                                    }
                                }

                                config.AddEnvironmentVariables();

                                if (args != null)
                                {
                                    config.AddCommandLine(args);
                                }
                            })
                            .UseStartup<Startup>()
                            .UseDefaultServiceProvider((context, options) =>
                            {
                                options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                            });
                    });
        }
    }
}