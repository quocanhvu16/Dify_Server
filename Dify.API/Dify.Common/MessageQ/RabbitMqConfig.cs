using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.MessageQ
{
    public class RabbitMqConfig
    {
        public string? ConnectionString { get; set; }

        public string? ExchangeName { get; set; }

        public string? QueueName { get; set; }

        public int? MaxConcurrentTask { get; set; }

        public int? StoppingTimeout { get; set; }
    }
}
