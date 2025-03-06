using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.MessageQ
{
    public interface IPublisher<T> where T : class
    {
        bool Publish(string key, T message);
        Task<bool> PublishAsync(string key, T message);
    }
}
