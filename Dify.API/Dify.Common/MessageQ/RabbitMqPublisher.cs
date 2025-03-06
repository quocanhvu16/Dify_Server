using EasyNetQ;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Dify.Common.MessageQ
{
    public class RabbitMqPublisher<T> : IPublisher<T>, IDisposable where T : class
    {

        #region Declaration

        private readonly RabbitMqConfig _config;
        private readonly IAdvancedBus _advanceBus;
        protected IModel m_chanel;

        private readonly string routingKey = "";

        Exchange exchange;

        #endregion

        #region Constructor

        public RabbitMqPublisher(RabbitMqConfig config, IAdvancedBus advancedBus)
        {
            _config = config;
            if(advancedBus != null)
            {
                _advanceBus = advancedBus;
            }
            else
            {
                _advanceBus = RabbitHutch.CreateBus(_config.ConnectionString).Advanced;
            }

            if (string.IsNullOrWhiteSpace(_config.ExchangeName))
            {
                exchange = Exchange.Default;
                routingKey = _config.QueueName;
                _advanceBus.QueueDeclare(_config.QueueName);
            }
            else
            {
                exchange = new Exchange(_config.ExchangeName);
            }
        }

        #endregion

        #region Method

        private IMessage<T> CreateMessage(string key, T message)
        {
            IMessage<T> rbMessage = new Message<T>(message);
                
            MessageProperties messageProperties = rbMessage.Properties;
            messageProperties.DeliveryMode = 2;
            messageProperties.CorrelationId = key;
            messageProperties.AppId = Assembly.GetEntryAssembly().FullName;

            messageProperties.Headers = new Dictionary<string, object>();
            messageProperties.Headers.Add("CreatedDate", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss:fffk"));

            return rbMessage;

        }

        public void Dispose()
        {
            if(_advanceBus != null && _advanceBus.IsConnected) _advanceBus.Dispose();
        }

        public bool Publish(string key, T message)
        {
            return PublishAsync(key, message).Result;
        }

        public async Task<bool> PublishAsync(string key, T message)
        {
            IMessage<T> rbMessage = CreateMessage(key, message);
            await _advanceBus.PublishAsync(exchange, routingKey, true, rbMessage);
            return true;
        }

        #endregion
    }
}
