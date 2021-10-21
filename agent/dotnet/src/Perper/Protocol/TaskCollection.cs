using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Perper.Protocol
{
    public class TaskCollection : ICollection<Task>
    {
        private struct EmptyStruct { }

        private readonly TaskCompletionSource<EmptyStruct> _completionSource = new();
        private readonly ConcurrentDictionary<Task, EmptyStruct> _tasks = new();
        private long _count;
        private bool _mayComplete;

        public int Count => (int)_count;
        public bool IsReadOnly => false;

        public TaskCollection() { }
        public TaskCollection(params Task[] tasks) : this((IEnumerable<Task>)tasks) { }
        public TaskCollection(IEnumerable<Task> tasks)
        {
            foreach (var task in tasks)
            {
                Add(task);
            }
        }

        public Task GetTask()
        {
            _mayComplete = true;

            if (Interlocked.Read(ref _count) == 0)
            {
                _completionSource.TrySetResult(new EmptyStruct());
            }

            return _completionSource.Task;
        }

        public void Add(Func<Task> taskFactory) // Sugar
        {
            Add(taskFactory());
        }

        public void Add(Task item)
        {
            _tasks[item] = new EmptyStruct();
            Interlocked.Increment(ref _count);
            item.ContinueWith(Remove, TaskScheduler.Default);
        }

        public bool Remove(Task item)
        {
            if (_tasks.TryRemove(item, out _))
            {
                if (item.IsFaulted)
                {
                    _completionSource.TrySetException(item.Exception!);
                }

                if (Interlocked.Decrement(ref _count) == 0 && _mayComplete)
                {
                    _completionSource.TrySetResult(new EmptyStruct());
                }

                return true;
            }
            return false;
        }

        public void Clear()
        {
            foreach (var task in _tasks.Keys)
            {
                Remove(task);
            }
        }

        public void CopyTo(Task[] array, int arrayIndex) => _tasks.Keys.CopyTo(array, arrayIndex);

        public bool Contains(Task item) => _tasks.ContainsKey(item);

        public IEnumerator<Task> GetEnumerator() => _tasks.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_tasks.Keys).GetEnumerator();
    }
}