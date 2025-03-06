using Dify.Common.MessageQ;
using EasyNetQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMqPublisher<T>(this IServiceCollection services, RabbitMqConfig config, IAdvancedBus advancedBus) where T : class
        {
            IPublisher<T> publisher = new RabbitMqPublisher<T>(config, advancedBus);
            services.AddSingleton(typeof(IPublisher<T>), publisher);

            return services;
        }

        public static IServiceCollection AddRabbitMqSubscriber<T>(this IServiceCollection services, RabbitMqConfig config, IAdvancedBus advancedBus) where T : class
        {
            //services.AddTransient(typeof(ISubscriber<T>), (arg) => { return new RabbitMqSubscriber<T>(config, advancedBus); });
            services.AddTransient(typeof(ISubscriber<T>), provider => new RabbitMqSubscriber<T>(config, advancedBus));
            return services;
        }
    }
}
