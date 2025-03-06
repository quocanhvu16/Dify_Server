using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.MessageQ
{
    /// <summary>
    /// quản lý số lượng task chạy đồng thời để tránh quá tải
    /// </summary>
    public class TaskManager
    {
        /// <summary>
        /// Số lượng task tối đa chạy cùng lúc
        /// </summary>
        private readonly int _max;

        /// <summary>
        /// Danh sách task đang chạy
        /// </summary>
        List<Tuple<string, Task>> _tasks = new List<Tuple<string, Task>>();

        public TaskManager(int max)
        {
            _max = max;
        }

        /// <summary>
        /// Chạy 1 task mới
        /// </summary>
        /// <param name="key"></param>
        /// <param name="action"></param>
        public void Run(string key, Action action)
        {
            if(string.IsNullOrWhiteSpace(key))
            {
                key = Guid.NewGuid().ToString("N");
            }

            // Tìm task có cùng key (nếu có) → chờ task đó chạy xong.
            Tuple<string, Task> existTask = _tasks.FirstOrDefault(f => f.Item1.Equals(key));

            // Tạo task mới và chạy.
            var tc = new Task(action);

            _tasks.Add(new Tuple<string, Task>(key, tc));

            if(existTask != null)
            {
                existTask.Item2.Wait();
            }

            tc.Start();

            RemoveCompletedTasks();

            if(_tasks.Count >= _max)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("Too many task...");

                Task[] runningTasks = _tasks.Select(s => s.Item2)?.ToArray();

                Task.WaitAny(runningTasks);

                RemoveCompletedTasks();
            }
        }

        /// <summary>
        ///  Xóa task đã xong
        /// </summary>
        private void RemoveCompletedTasks()
        {
            int lastIDs = _tasks.Count - 1;

            for(int i = lastIDs; i >= 0; i--)
            {
                if (_tasks[i].Item2.IsCompleted)
                {
                    _tasks.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Chờ tất cả task chạy xong
        /// </summary>
        /// <param name="timeout"></param>
        public void WaitAll(TimeSpan timeout)
        {
            Task[] runningTasks = _tasks.Select(s => s.Item2)?.ToArray();
            if(runningTasks != null && runningTasks.Length > 0)
            {
                Task.WaitAll(runningTasks);
            }
        }
    }
}
