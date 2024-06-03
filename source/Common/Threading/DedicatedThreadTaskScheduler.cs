using System.Collections.Concurrent;

namespace Common.Threading;

/// <summary>
/// Executes all <see cref="Task"/> continuations on a dedicated thread.
/// </summary>
public sealed class DedicatedThreadTaskScheduler : TaskScheduler, IDisposable
{
    private readonly BlockingCollection<Task> _taskQueue;
    private readonly CancellationTokenSource _cts;
    private readonly Thread _thread;
    private bool _disposed;

    public DedicatedThreadTaskScheduler()
    {
        _taskQueue = new BlockingCollection<Task>();
        _cts = new CancellationTokenSource();
        _thread = new Thread(Run);

        _thread.Start();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
        }
    }

    private void Run()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                Task task = _taskQueue.Take(_cts.Token);
                TryExecuteTask(task);
            }
            catch (OperationCanceledException)
            {
                // an operation which the thread was running has been cancelled 
            }
        }
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return _taskQueue;
    }

    protected override void QueueTask(Task task)
    {
        _taskQueue.Add(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (Thread.CurrentThread == _thread)
        {
            return TryExecuteTask(task);
        }

        return false;
    }
}
