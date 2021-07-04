using System;
using System.Threading;

namespace Schott.Async
{
  /// <summary>
  /// Switches to another <see cref="SynchronizationContext"/> and back to the original when disposed.
  /// </summary>
  public sealed class SynchronizationContextRegion : IDisposable
  {
    private readonly SynchronizationContext? priorSynchronisationContext;

    /// <summary>
    /// Switches to no synchronization context, i.e. continuations are placed on a "random" threadpool thread.
    /// </summary>
    public static readonly IDisposable None = new SynchronizationContextRegion(null);

    public SynchronizationContextRegion(SynchronizationContext? synchronizationContext)
    {
      priorSynchronisationContext = SynchronizationContext.Current;
      SynchronizationContext.SetSynchronizationContext(synchronizationContext);
    }

    public void Dispose()
    {
      SynchronizationContext.SetSynchronizationContext(priorSynchronisationContext);
    }
  }
}