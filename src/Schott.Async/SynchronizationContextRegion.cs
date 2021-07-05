using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Schott.Async
{
  /// <summary>
  ///   Switches to another <see cref="SynchronizationContext" /> and back to the original when disposed.
  /// </summary>
  public sealed class SynchronizationContextRegion : IDisposable, INotifyCompletion
  {
    private readonly SynchronizationContext? priorSynchronisationContext;
    private readonly SynchronizationContext? synchronizationContext;

    public SynchronizationContextRegion(SynchronizationContext? synchronizationContext)
    {
      this.synchronizationContext = synchronizationContext;
      priorSynchronisationContext = SynchronizationContext.Current;
      SynchronizationContext.SetSynchronizationContext(synchronizationContext);
    }

    public bool IsCompleted => SynchronizationContext.Current != synchronizationContext;

    /// <summary>
    ///   Switches to no synchronization context, i.e. continuations are placed on a "random" threadpool thread.
    /// </summary>
    public static IDisposable None() => new SynchronizationContextRegion(null);

    public SynchronizationContextRegion GetAwaiter() => this;

    public void Dispose()
    {
      SynchronizationContext.SetSynchronizationContext(priorSynchronisationContext);
    }

    public void OnCompleted(Action continuation)
    {
      using (this)
      {
        continuation();
      }
    }

    public void GetResult()
    {
    }
  }
}