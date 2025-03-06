using EasyNetQ;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.MessageQ
{
    public class RabbitMqSubscriber<T> : ISubscriber<T>, IDisposable where T : class
    {
        #region Declaration

        private readonly RabbitMqConfig _config;

        /// <summary>
        /// Đối tượng IAdvancedBus của EasyNetQ để giao tiếp với RabbitMQ
        /// </summary>
        private readonly IAdvancedBus _advancedBus;

        /// <summary>
        /// Đối tượng Disposable để dừng consumer khi cần
        /// </summary>
        IDisposable _consumerDisposable;

        /// <summary>
        /// Task dùng để chặn việc subscribe khi dừng
        /// </summary>
        private Task _stoppingTask = null;

        /// <summary>
        /// 	Đối tượng quản lý số lượng task đang chạy
        /// </summary>
        private TaskManager _taskManager = null;

        #endregion

        #region Constructor

        public RabbitMqSubscriber(RabbitMqConfig config, IAdvancedBus advancedBus)
        {
            _config = config;
            if(advancedBus != null)
            {
                _advancedBus = advancedBus;
            }
            else
            {
                _advancedBus = RabbitHutch.CreateBus(_config.ConnectionString).Advanced;
            }
        }

        ~RabbitMqSubscriber()
        {
            Dispose();
        }

        #endregion
        #region Method

        /// <summary>
        /// Dừng Subscriber
        /// </summary>
        public void Dispose()
        {
            UnSubscribe();
            // Đóng kết nối rabbitMQ
            if(_advancedBus != null && _advancedBus.IsConnected)
            {
                _advancedBus.Dispose();
            }
        }

        /// <summary>
        /// Lắng nghe message từ RabbitMQ
        /// </summary>
        public void Subscribe(Func<string, T, Task> handler, CancellationToken cancellationToken = default)
        {
            int maxConcurrentTask = _config.MaxConcurrentTask.Value;
            _taskManager = new TaskManager(maxConcurrentTask);

            var consumerQueue = _advancedBus.QueueDeclare(_config.QueueName);
            _consumerDisposable = _advancedBus.Consume(consumerQueue, (body, props, messInfo) =>
            {
                if (_stoppingTask != null) _stoppingTask.Wait();

                string key = props.CorrelationId;

                string jsonText = System.Text.Encoding.UTF8.GetString(body.Span);

                T message = JsonConvert.DeserializeObject<T>(jsonText);

                _taskManager.Run(key, () => handler(key, message).Wait());

            });
        }

        /// <summary>
        /// Hủy lắng nghe RabbitMQ
        /// </summary>
        public void UnSubscribe(TimeSpan? timeout = null)
        {
            if(timeout == null)
            {
                timeout = TimeSpan.FromSeconds(_config.StoppingTimeout.Value);
            }
            _stoppingTask = new Task(() => { });

            if (_taskManager != null) _taskManager.WaitAll(timeout.Value);
            if(_consumerDisposable != null) _consumerDisposable.Dispose();
        }

        #endregion
    }
}
