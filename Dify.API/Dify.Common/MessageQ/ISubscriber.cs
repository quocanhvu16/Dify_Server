using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.MessageQ
{
    public interface ISubscriber<T> where T : class
    {
        void Subscribe(Func<string, T, Task> action, CancellationToken cancellationToken = default);

        void UnSubscribe(TimeSpan? timeout = null);
    }
}
