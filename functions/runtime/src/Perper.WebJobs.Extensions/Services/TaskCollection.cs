using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Services
{
    public class TaskCollection
    {
        private struct EmptyStruct {}

        private TaskCompletionSource<EmptyStruct> _completionSource = new TaskCompletionSource<EmptyStruct>();
        private ConcurrentDictionary<Task, EmptyStruct> _tasks = new ConcurrentDictionary<Task, EmptyStruct>();
        private long _count = 0;
        private bool _mayComplete = false;

        public Task GetTask()
        {
            _mayComplete = true;
            if (_count == 0)
            {
                _completionSource.TrySetResult(new EmptyStruct());
            }

            return _completionSource.Task;
        }

        public void Add(Func<Task> taskFactory) // Sugar
        {
            Add(taskFactory());
        }

        public void Add(Task task)
        {
            _tasks[task] = new EmptyStruct();
            Interlocked.Increment(ref _count);
            task.ContinueWith(Remove);
        }

        public void Remove(Task task)
        {
            if (_tasks.TryRemove(task, out var _value))
            {
                if (task.IsFaulted)
                {
                    _completionSource.TrySetException(task.Exception!);
                }

                if (Interlocked.Decrement(ref _count) == 0 && _mayComplete)
                {
                    _completionSource.TrySetResult(new EmptyStruct());
                }
            }
        }
    }
}