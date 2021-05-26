using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Services
{
    public class TaskCollection
    {
        private struct EmptyStruct { }

        private readonly TaskCompletionSource<EmptyStruct> _completionSource = new TaskCompletionSource<EmptyStruct>();
        private readonly ConcurrentDictionary<Task, EmptyStruct> _tasks = new ConcurrentDictionary<Task, EmptyStruct>();
        private long _count;
        private bool _mayComplete;

        public Task GetTask()
        {
            _mayComplete = true;

            if (Interlocked.Read(ref _count) == 0)
            {
                _completionSource.TrySetResult(new EmptyStruct());
            }

            return _completionSource.Task;
        }

        // Sugar
        public void Add(Func<Task> taskFactory) => Add(taskFactory());

        public void Add(Task task)
        {
            _tasks[task] = new EmptyStruct();
            Interlocked.Increment(ref _count);
            task.ContinueWith(Remove);
        }

        public void Remove(Task task)
        {
            if (_tasks.TryRemove(task, out _))
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