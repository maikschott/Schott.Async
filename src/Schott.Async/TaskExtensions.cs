using System;
using System.Threading;
using System.Threading.Tasks;

namespace Schott.Async
{
  public static class TaskExtensions
  {
    // Based on Gérald Barré: "Fire and forget a Task in .NET" https://www.meziantou.net/fire-and-forget-a-task-in-dotnet.htm
    /// <summary>
    /// Forget about a fired task, i.e. in cases where awaiting a task isn't needed.
    /// </summary>
    /// <param name="task">The task to forget</param>
    /// <remarks>Exceptions will be swallowed.</remarks>
    public static void Forget(this Task? task)
    {
      if (task is null || (task.IsCompleted && !task.IsFaulted)) { return; }

      _ = AwaitTask(task);

      static async Task AwaitTask(Task task)
      {
        try
        {
          await task.ConfigureAwait(false);
        }
        catch
        {
          // Ignore exception
        }
      }
    }

    /// <summary>
    /// Converts a <see cref="WaitHandle"/> to a task.
    /// </summary>
    /// <param name="waitHandle">The wait handle</param>
    /// <param name="timeout">Optional timeout</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>
    /// If the <paramref name="waitHandle"/> was set, a task with the result <see langword="true"/> is returned.
    /// If the <paramref name="timeout"/> was reached, a task with the result <see langword="false"/> is returned.
    /// If the <paramref name="cancellationToken"/> was set to cancelled, a cancelled task will be returned.
    /// </returns>
    public static Task<bool> ToTask(this WaitHandle waitHandle, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
      if (waitHandle is null) { throw new ArgumentNullException(nameof(waitHandle)); }

      if (waitHandle.WaitOne(TimeSpan.Zero)) { return TaskResult.True; }

      if (timeout == TimeSpan.Zero) { return TaskResult.False; }

      if (cancellationToken.IsCancellationRequested) { return Task.FromCanceled<bool>(cancellationToken); }

      return ToTaskInternal(waitHandle, timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);

      static async Task<bool> ToTaskInternal(WaitHandle waitHandle, TimeSpan timeout, CancellationToken cancellationToken)
      {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        var registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(waitHandle, (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(!timedOut), taskCompletionSource, timeout, true);
        try
        {
          using var cancellationTokenRegistration = cancellationToken.Register(state =>
          {
            var tuple = ((TaskCompletionSource<bool> taskCompletionSource, CancellationToken cancellationToken))state!;
            tuple.taskCompletionSource.TrySetCanceled(tuple.cancellationToken);
          }, (taskCompletionSource, cancellationToken));

          return await taskCompletionSource.Task.ConfigureAwait(false);
        }
        finally
        {
          registeredWaitHandle.Unregister(null);
        }
      }
    }

    /// <summary>
    /// Returns a task the completes when either the provided <paramref name="task"/> task completes or the <paramref name="cancellationToken"/> was set to cancelled.
    /// </summary>
    /// <param name="task">The task to extend with a cancellation token</param>
    /// <param name="cancellationToken">The cancellation token to extend the task with</param>
    /// <returns>
    /// If the <paramref name="task"/> completes first, this task will be returned.
    /// If the <paramref name="cancellationToken"/> is set to cancelled first, a cancelled task will be returned.
    /// </returns>
    public static Task WithCancellation(this Task task, CancellationToken cancellationToken)
    {
      if (task is null) { throw new ArgumentNullException(nameof(task)); }

      if (!cancellationToken.CanBeCanceled || task.IsCompleted) { return task; }

      if (cancellationToken.IsCancellationRequested) { return Task.FromCanceled(cancellationToken); }

      return WithCancellationInternal();

      async Task WithCancellationInternal()
      {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (cancellationToken.Register(state => ((TaskCompletionSource<bool>)state!).TrySetResult(false), tcs).ConfigureAwait(false))
        {
          var resultTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
          if (resultTask == tcs.Task)
          {
            throw new OperationCanceledException(cancellationToken);
          }

          await task.ConfigureAwait(false);
        }
      }
    }

    /// <summary>
    /// Returns a task the completes when either the provided <paramref name="task"/> task completes or the <paramref name="cancellationToken"/> was set to cancelled.
    /// </summary>
    /// <param name="task">The task to extend with a cancellation token</param>
    /// <param name="cancellationToken">The cancellation token to extend the task with</param>
    /// <returns>
    /// If the <paramref name="task"/> completes first, this task will be returned.
    /// If the <paramref name="cancellationToken"/> is set to cancelled first, a cancelled task will be returned.
    /// </returns>
    public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
      if (task is null) { throw new ArgumentNullException(nameof(task)); }

      if (!cancellationToken.CanBeCanceled || task.IsCompleted) { return task; }

      if (cancellationToken.IsCancellationRequested) { return Task.FromCanceled<T>(cancellationToken); }

      return WithCancellationInternal();

      async Task<T> WithCancellationInternal()
      {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (cancellationToken.Register(state => ((TaskCompletionSource<bool>)state!).TrySetResult(false), tcs).ConfigureAwait(false))
        {
          var resultTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
          if (resultTask == tcs.Task)
          {
            throw new OperationCanceledException(cancellationToken);
          }

          return await task.ConfigureAwait(false);
        }
      }
    }

    /// <summary>
    /// Returns a task the completes when either the provided <paramref name="task"/> task completes or the <paramref name="timeout"/> expires.
    /// </summary>
    /// <param name="task">The task to extend with a timeout</param>
    /// <param name="timeout">The timeout to extend the task with</param>
    /// <returns>
    /// If the <paramref name="task"/> completes first, this task will be returned.
    /// If the <paramref name="timeout"/> expires, a faulted task with an <see cref="TimeoutException"/> will be returned.
    /// </returns>
    public static async Task WithTimeout(this Task task, TimeSpan timeout)
    {
      if (task is null) { throw new ArgumentNullException(nameof(task)); }

      using var cts = new CancellationTokenSource();

      var delayTask = Task.Delay(timeout, cts.Token);
      var resultTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);
      if (resultTask == delayTask)
      {
        throw new TimeoutException();
      }

      // Task completed -> timer isn't needed anymore
      cts.Cancel();

      await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a task the completes when either the provided <paramref name="task"/> task completes or the <paramref name="timeout"/> expires.
    /// </summary>
    /// <param name="task">The task to extend with a timeout</param>
    /// <param name="timeout">The timeout to extend the task with</param>
    /// <returns>
    /// If the <paramref name="task"/> completes first, this task will be returned.
    /// If the <paramref name="timeout"/> expires, a faulted task with an <see cref="TimeoutException"/> will be returned.
    /// </returns>
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
      if (task is null) { throw new ArgumentNullException(nameof(task)); }

      using var cts = new CancellationTokenSource();

      var delayTask = Task.Delay(timeout, cts.Token);
      var resultTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);
      if (resultTask == delayTask)
      {
        throw new TimeoutException();
      }

      // Task completed -> timer isn't needed anymore
      cts.Cancel();

      return await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Safer way to await a task synchronously.
    /// The task is wrapped in another thread-bound task do avoid synchronization context deadlocks,
    /// and awaited using <see cref="Task.GetAwaiter"/> so that in case of an exception it doesn't get wrapped in an <see cref="AggregateException"/>.
    /// </summary>
    /// <param name="task">The task to await synchronously</param>
    public static void SaferWait(this Task task)
    {
      Task.Run(() => task).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Safer way to await a task synchronously.
    /// The task is wrapped in another thread-bound task do avoid synchronization context deadlocks,
    /// and awaited using <see cref="Task.GetAwaiter"/> so that in case of an exception it doesn't get wrapped in an <see cref="AggregateException"/>.
    /// </summary>
    /// <param name="task">The task to await synchronously</param>
    public static T SaferResult<T>(this Task<T> task)
    {
      return Task.Run(() => task).GetAwaiter().GetResult();
    }
  }
}