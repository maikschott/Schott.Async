using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Schott.Async
{
  // The implementation is based on: Stephen Toub: "Implementing a simple ForEachAsync, part 2". https://devblogs.microsoft.com/pfxteam/implementing-a-simple-foreachasync-part-2/
  /// <summary>
  ///   Provides support for async parallel loops.
  /// </summary>
  public static class ParallelAsync
  {
    private static readonly ParallelOptions defaultParallelOptions = new ParallelOptions();

    /// <summary>
    ///   Executes a for loop in which iterations may run in parallel and loop options can be configured.
    /// </summary>
    /// <param name="fromInclusive">The start index, inclusive.</param>
    /// <param name="toExclusive">The end index, exclusive.</param>
    /// <param name="parallelOptions">An object that configures the behavior of this operation.</param>
    /// <param name="body">The async delegate that is invoked once per iteration.</param>
    /// <returns>A task that represents the completion of all iterations.</returns>
    public static Task ForAsync(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Func<int, Task> body)
    {
      if (parallelOptions == null) { throw new ArgumentNullException(nameof(parallelOptions)); }

      if (body == null) { throw new ArgumentNullException(nameof(body)); }

      if (toExclusive <= fromInclusive) { return Task.CompletedTask; }

      var taskScheduler = parallelOptions.TaskScheduler ?? TaskScheduler.Current; // see ParallelOptions.EffectiveTaskScheduler
      var maxDegreeOfParallelism = GetEffectiveMaxDegreeOfParallelism(taskScheduler, parallelOptions.MaxDegreeOfParallelism);
      var cancellationToken = parallelOptions.CancellationToken;

      return Task.WhenAll(Partitioner.Create(fromInclusive, toExclusive).GetPartitions(maxDegreeOfParallelism).Select(partition =>
        Task.Factory.StartNew(async () =>
          {
            using (partition)
            {
              while (partition.MoveNext())
              {
                var slice = partition.Current;
                if (slice == null) { continue; }

                for (var i = slice.Item1; i < slice.Item2; i++)
                {
                  cancellationToken.ThrowIfCancellationRequested();
                  await body(i);
                }
              }
            }
          },
          cancellationToken, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning, taskScheduler).Unwrap()));
    }

    /// <summary>
    ///   Executes a for loop in which iterations may run in parallel.
    /// </summary>
    /// <param name="fromInclusive">The start index, inclusive.</param>
    /// <param name="toExclusive">The end index, exclusive.</param>
    /// <param name="body">The async delegate that is invoked once per iteration.</param>
    /// <returns>A task that represents the completion of all iterations.</returns>
    public static Task ForAsync(int fromInclusive, int toExclusive, Func<int, Task> body) => ForAsync(fromInclusive, toExclusive, defaultParallelOptions, body);

    /// <summary>
    ///   Executes a foreach operation on an IEnumerable in which iterations may run in parallel and loop options can be
    ///   configured.
    /// </summary>
    /// <typeparam name="TSource">The type of the data in the source.</typeparam>
    /// <param name="source">An enumerable data source.</param>
    /// <param name="parallelOptions">An object that configures the behavior of this operation.</param>
    /// <param name="body">The async delegate that is invoked once per iteration.</param>
    /// <returns>A task that represents the completion of all iterations.</returns>
    public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, Task> body)
    {
      if (source == null) { throw new ArgumentNullException(nameof(source)); }

      if (parallelOptions == null) { throw new ArgumentNullException(nameof(parallelOptions)); }

      if (body == null) { throw new ArgumentNullException(nameof(body)); }

      var taskScheduler = parallelOptions.TaskScheduler ?? TaskScheduler.Current; // see ParallelOptions.EffectiveTaskScheduler
      var maxDegreeOfParallelism = GetEffectiveMaxDegreeOfParallelism(taskScheduler, parallelOptions.MaxDegreeOfParallelism);
      var cancellationToken = parallelOptions.CancellationToken;

      return Task.WhenAll(Partitioner.Create(source).GetPartitions(maxDegreeOfParallelism).Select(partition =>
        Task.Factory.StartNew(async () =>
          {
            using (partition)
            {
              while (partition.MoveNext())
              {
                cancellationToken.ThrowIfCancellationRequested();
                await body(partition.Current);
              }
            }
          },
          cancellationToken, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning, taskScheduler).Unwrap()));
    }

    /// <summary>
    ///   Executes a foreach operation on an IEnumerable in which iterations may run in parallel.
    /// </summary>
    /// <typeparam name="TSource">The type of the data in the source.</typeparam>
    /// <param name="source">An enumerable data source.</param>
    /// <param name="body">The async delegate that is invoked once per iteration.</param>
    /// <returns>A task that represents the completion of all iterations.</returns>
    public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, Func<TSource, Task> body) => ForEachAsync(source, defaultParallelOptions, body);

    private static int GetEffectiveMaxDegreeOfParallelism(TaskScheduler taskScheduler, int maxDegreeOfParallelism)
    {
      if (maxDegreeOfParallelism == -1) { return Environment.ProcessorCount; }

      // see ParallelOptions.EffectiveMaxConcurrencyLevel
      var result = maxDegreeOfParallelism;
      var schedulerMax = taskScheduler.MaximumConcurrencyLevel;
      if (schedulerMax > 0 && schedulerMax != int.MaxValue)
      {
        result = Math.Min(schedulerMax, result);
      }

      return result;
    }
  }
}