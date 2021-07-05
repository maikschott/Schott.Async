using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Schott.Async
{
  /// <summary>
  ///   When awaited, switches to another <see cref="SynchronizationContext" /> and back to the original when the calling method completes.
  /// </summary>
  public sealed class AwaitableSynchronizationContextRegion : INotifyCompletion
  {
    private readonly SynchronizationContext? synchronizationContext;

    public AwaitableSynchronizationContextRegion(SynchronizationContext? synchronizationContext)
    {
      this.synchronizationContext = synchronizationContext;
    }

    public bool IsCompleted => SynchronizationContext.Current == synchronizationContext;

    /// <summary>
    ///   Switches to no synchronization context, i.e. continuations are placed on a "random" threadpool thread.
    /// </summary>
    public static AwaitableSynchronizationContextRegion None() => new AwaitableSynchronizationContextRegion(null);

    public AwaitableSynchronizationContextRegion GetAwaiter() => this;

    public void OnCompleted(Action continuation)
    {
      using (new SynchronizationContextRegion(synchronizationContext))
      {
        continuation();
      }
    }

    public void GetResult()
    {
    }
  }
}