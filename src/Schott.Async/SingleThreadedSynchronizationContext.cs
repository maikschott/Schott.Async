using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Schott.Async
{
  /// <summary>
  ///   A single-threaded synchronization context where each message is dispatched to the same thread.
  /// </summary>
  public sealed class SingleThreadedSynchronizationContext : SynchronizationContext, IDisposable
  {
    private readonly BlockingCollection<(SendOrPostCallback d, object? state, ManualResetEventSlim? e)> queue;
    private readonly int threadId;
    private int operationCount;

    public SingleThreadedSynchronizationContext()
    {
      queue = new BlockingCollection<(SendOrPostCallback d, object? state, ManualResetEventSlim? e)>();
      threadId = Environment.CurrentManagedThreadId;
    }

    public override void OperationStarted()
    {
      Interlocked.Increment(ref operationCount);
    }

    public override void OperationCompleted()
    {
      if (Interlocked.Decrement(ref operationCount) == 0)
      {
        queue.CompleteAdding();
      }
    }

    public override void Post(SendOrPostCallback callback, object? state)
    {
      queue.Add((callback, state, null));
    }

    public override void Send(SendOrPostCallback callback, object? state)
    {
      if (Environment.CurrentManagedThreadId == threadId)
      {
        callback(state);
        return;
      }

      using var callbackCompletedEvent = new ManualResetEventSlim(false);
      queue.Add((callback, state, callbackCompletedEvent));
      callbackCompletedEvent.Wait();
    }

    /// <summary>
    ///   Sets up the synchronization context and a task to process the dispatched messages on.
    /// </summary>
    /// <param name="action">An asynchronous action where each continuation is dispatched to this synchronization context.</param>
    /// <returns></returns>
    public static async Task RunAsync(Func<Task> action)
    {
      var syncCtx = new SingleThreadedSynchronizationContext();
      using (new SynchronizationContextRegion(syncCtx))
      {
        using (syncCtx)
        {
          syncCtx.OperationStarted();
          var actionTask = action().ContinueWith(_ => syncCtx.OperationCompleted(), TaskScheduler.Default);
          syncCtx.ProcessMessages();
          await actionTask;
        }
      }
    }

    public void ProcessMessages()
    {
      foreach (var (callback, state, waitEvent) in queue.GetConsumingEnumerable())
      {
        callback.Invoke(state);
        waitEvent?.Set();
      }
    }

    public override SynchronizationContext CreateCopy() => this;

    public void Dispose()
    {
      foreach (var (_, _, waitEvent) in queue)
      {
        waitEvent?.Dispose();
      }

      queue.Dispose();
    }
  }
}