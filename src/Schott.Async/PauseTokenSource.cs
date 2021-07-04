using System;
using System.Threading;

namespace Schott.Async
{
  // Based on https://devblogs.microsoft.com/pfxteam/cooperatively-pausing-async-methods/, but implemented using a ManualResetEventSlim.
  /// <summary>
  /// Signals to a <see cref="PauseToken"/> that it should be paused / unpaused.
  /// </summary>
  public sealed class PauseTokenSource : IDisposable
  {
    private readonly ManualResetEventSlim manualResetEvent;

    public PauseTokenSource()
    {
      manualResetEvent = new ManualResetEventSlim(true);
    }

    /// <summary>
    /// Gets the <see cref="PauseToken"/> associated with this <see cref="PauseTokenSource"/>.
    /// </summary>
    public PauseToken Token => new PauseToken(manualResetEvent);

    /// <summary>
    /// Transition the associated <see cref="PauseToken"/> into the paused / unpaused state.
    /// </summary>
    public bool IsPaused
    {
      get => !manualResetEvent.IsSet;
      set
      {
        if (value)
        {
          manualResetEvent.Reset();
        }
        else
        {
          manualResetEvent.Set();
        }
      }
    }

    /// <summary>
    /// Releases the resources used by this <see cref="PauseTokenSource" />.
    /// </summary>
    public void Dispose()
    {
      manualResetEvent.Dispose();
    }
  }
}