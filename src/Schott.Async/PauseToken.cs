using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Schott.Async
{
  /// <summary>
  /// Propagates notification that operations should be paused.
  /// </summary>
  [DebuggerDisplay("IsPauseRequested = {IsPauseRequested}")]
  public readonly struct PauseToken
  {
    private readonly ManualResetEventSlim? manualResetEvent;

    /// <summary>
    /// Returns an empty pause token.
    /// </summary>
    public static readonly PauseToken None = default;

    internal PauseToken(ManualResetEventSlim manualResetEvent)
    {
      this.manualResetEvent = manualResetEvent;
    }

    /// <summary>
    /// Gets whether this token is capable of being in the paused state.
    /// </summary>
    /// <remarks>
    /// If CanBePaused returns false, it is guaranteed that the token will never transition
    /// into a paused state, meaning that <see cref="IsPauseRequested"/> will never
    /// return true.
    /// </remarks>
    public bool CanBePaused => manualResetEvent != null;

    /// <summary>
    /// Gets whether pausing has been requested for this token.
    /// </summary>
    public bool IsPauseRequested => manualResetEvent?.IsSet == false;

    /// <summary>
    /// Synchronously waits until <see cref="IsPauseRequested"/> transitions into the non-paused state,
    /// or the <paramref name="cancellationToken"/> is set to the cancelled state.
    /// </summary>
    /// <param name="cancellationToken">The optional cancellation token</param>
    public void WaitWhilePaused(CancellationToken cancellationToken = default)
    {
      manualResetEvent?.Wait(cancellationToken);
    }

    /// <summary>
    /// Asynchronously waits until <see cref="IsPauseRequested"/> transitions into the non-paused state,
    /// or the <paramref name="cancellationToken"/> is set to the cancelled state.
    /// </summary>
    /// <param name="cancellationToken">The optional cancellation token</param>
    public Task WaitWhilePausedAsync(CancellationToken cancellationToken = default)
    {
      if (manualResetEvent is null) { return Task.CompletedTask; }

      return manualResetEvent.WaitHandle.ToTask(cancellationToken: cancellationToken);
    }
  }
}