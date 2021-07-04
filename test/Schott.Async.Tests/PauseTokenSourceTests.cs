using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Schott.Async.Tests
{
  public class PauseTokenSourceTests : IDisposable
  {
    private readonly PauseTokenSource pauseTokenSource;

    public PauseTokenSourceTests()
    {
      pauseTokenSource = new PauseTokenSource();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IsPaused_ChangesState(bool isPaused)
    {
      pauseTokenSource.IsPaused = isPaused;

      Assert.Equal(isPaused, pauseTokenSource.IsPaused);
    }

    [Fact]
    public void PauseToken_None_CanBePaused()
    {
      Assert.False(PauseToken.None.CanBePaused);
    }

    [Fact]
    public void PauseToken_None_IsPauseRequested()
    {
      Assert.False(PauseToken.None.IsPauseRequested);
    }

    [Fact]
    public void PauseToken_None_WaitWhilePaused()
    {
      var safeGuardCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

      PauseToken.None.WaitWhilePaused(safeGuardCts.Token);
    }

    [Fact]
    public async Task PauseToken_None_WaitWhilePausedAsync()
    {
      var safeGuardCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

      await PauseToken.None.WaitWhilePausedAsync(safeGuardCts.Token);
    }

    [Fact]
    public void Token_CanBePaused()
    {
      Assert.True(pauseTokenSource.Token.CanBePaused);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Token_IsPauseRequested_TokenBefore(bool isPaused)
    {
      var pauseToken = pauseTokenSource.Token;
      pauseTokenSource.IsPaused = isPaused;
      Assert.Equal(isPaused, pauseToken.IsPauseRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Token_IsPauseRequested_TokenAfter(bool isPaused)
    {
      pauseTokenSource.IsPaused = isPaused;
      var pauseToken = pauseTokenSource.Token;
      Assert.Equal(isPaused, pauseToken.IsPauseRequested);
    }

    [Fact]
    public async Task Token_WaitWhilePaused()
    {
      var safeGuardCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

      // should return immediately
      pauseTokenSource.IsPaused = false;
      pauseTokenSource.Token.WaitWhilePaused(safeGuardCts.Token);

      // should return after IsPaused is set
      await CheckIfPaused(() =>
      {
        pauseTokenSource.Token.WaitWhilePaused(safeGuardCts.Token);
        return Task.CompletedTask;
      });

      // should return immediately
      pauseTokenSource.Token.WaitWhilePaused(safeGuardCts.Token);

      // should return after IsPaused is set
      await CheckIfPaused(() =>
      {
        pauseTokenSource.Token.WaitWhilePaused(safeGuardCts.Token);
        return Task.CompletedTask;
      });
    }

    [Fact]
    public async Task Token_WaitWhilePausedAsync()
    {
      var safeGuardCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

      // should return immediately
      pauseTokenSource.IsPaused = false;
      await pauseTokenSource.Token.WaitWhilePausedAsync(safeGuardCts.Token);

      // should return after IsPaused is set
      await CheckIfPaused(() => pauseTokenSource.Token.WaitWhilePausedAsync(safeGuardCts.Token));

      // should return immediately
      await pauseTokenSource.Token.WaitWhilePausedAsync(safeGuardCts.Token);

      // should return after IsPaused is set
      await CheckIfPaused(() => pauseTokenSource.Token.WaitWhilePausedAsync(safeGuardCts.Token));
    }

    private async Task CheckIfPaused(Func<Task> waitWhilePausedDelegate)
    {
      const int Delay = 100;
      var watch = Stopwatch.StartNew();

      pauseTokenSource.IsPaused = true;
      var delayTask = Task.Delay(Delay).ContinueWith(_ => pauseTokenSource.IsPaused = false);
      var task = waitWhilePausedDelegate();
      await Task.WhenAll(task, delayTask);

      watch.Stop();

      Assert.True(watch.ElapsedMilliseconds >= Delay);
    }

    void IDisposable.Dispose() => pauseTokenSource.Dispose();
  }
}