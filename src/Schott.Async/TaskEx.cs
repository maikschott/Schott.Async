using System;
using System.Threading;
using System.Threading.Tasks;

namespace Schott.Async
{
  /// <summary>
  /// <c>Task.Run</c> with <see cref="TaskCreationOptions"/>.
  /// </summary>
  public static class TaskEx
  {
    /// <summary>
    /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/> handle for that work.
    /// </summary>
    /// <param name="function">The work to execute asynchronously</param>
    /// <param name="taskCreationOptions">A <see cref="TaskCreationOptions"/> value that controls the behavior of the created <see cref="Task"/></param>
    /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
    /// <returns>A <see cref="Task"/> that represents the work queued to execute in the thread pool.</returns>
    public static Task Run(Action function, TaskCreationOptions taskCreationOptions, CancellationToken cancellationToken = default)
    {
      return Task.Factory.StartNew(function, cancellationToken, taskCreationOptions | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    /// <summary>
    /// Queues the specified work to run on the thread pool and returns a <see cref="Task{TResult}"/> handle for that work.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="function">The work to execute asynchronously</param>
    /// <param name="taskCreationOptions">A <see cref="TaskCreationOptions"/> value that controls the behavior of the created <see cref="Task{TResult}"/></param>
    /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the work queued to execute in the thread pool.</returns>
    public static Task<TResult> Run<TResult>(Func<TResult> function, TaskCreationOptions taskCreationOptions, CancellationToken cancellationToken = default)
    {
      return Task.Factory.StartNew(function, cancellationToken, taskCreationOptions | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    /// <summary>
    /// Queues the specified work to run on the thread pool and returns a proxy for the <see cref="Task"/> returned by <paramref name="function"/>.
    /// </summary>
    /// <param name="function">The work to execute asynchronously</param>
    /// <param name="taskCreationOptions">A <see cref="TaskCreationOptions"/> value that controls the behavior of the created <see cref="Task"/>.</param>
    /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
    /// <returns>A task that represents a proxy for the task returned by <paramref name="function"/>.</returns>
    public static Task Run(Func<Task> function, TaskCreationOptions taskCreationOptions, CancellationToken cancellationToken = default)
    {
      return Task<Task>.Factory.StartNew(function, cancellationToken, taskCreationOptions | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
    }

    /// <summary>
    /// Queues the specified work to run on the thread pool and returns a proxy for the <see cref="Task{TResult}"/> returned by <paramref name="function"/>.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="function">The work to execute asynchronously</param>
    /// <param name="taskCreationOptions">A <see cref="TaskCreationOptions"/> value that controls the behavior of the created <see cref="Task{TResult}"/></param>
    /// <param name="cancellationToken">A cancellation token that should be used to cancel the work</param>
    /// <returns>A task that represents a proxy for the task returned by <paramref name="function"/>.</returns>
    public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, TaskCreationOptions taskCreationOptions, CancellationToken cancellationToken = default)
    {
      return Task<Task<TResult>>.Factory.StartNew(function, cancellationToken, taskCreationOptions | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
    }
  }
}
