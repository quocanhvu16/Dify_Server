using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.Middleware
{
    public class SetupRequiredMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public SetupRequiredMiddleware(RequestDelegate next, IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            //using var scope = _scopeFactory.CreateScope();
            //var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            //var initPassword = _configuration["INIT_PASSWORD"];
            //var isSelfHosted = _configuration["EDITION"] == "SELF_HOSTED";

            //if (isSelfHosted && !string.IsNullOrEmpty(initPassword) && !dbContext.DifySetups.Any())
            //{
            //    context.Response.StatusCode = StatusCodes.Status403Forbidden;
            //    await context.Response.WriteAsync("Application is not initialized.");
            //    return;
            //}

            await _next(context);
        }
    }
}
