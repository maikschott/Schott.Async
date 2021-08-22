using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Schott.Async.Tests
{
  public class ParallelAsyncTests
  {
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    public void ForAsync_NothingToIterate(int from, int to)
    {
      var wasIterated = false;
      var task = ParallelAsync.ForAsync(from, to, _ =>
      {
        wasIterated = true;
        return Task.CompletedTask;
      });
      
      Assert.True(task.IsCompletedSuccessfully);
      Assert.False(wasIterated);
    }

    [Fact]
    public void ForAsync_ParallelOptions_Null()
    {
      Assert.ThrowsAsync<ArgumentNullException>("parallelOptions", () => ParallelAsync.ForAsync(0, 1, null!, _ => Task.CompletedTask));
    }

    [Fact]
    public void ForAsync_Body_Null()
    {
      Assert.ThrowsAsync<ArgumentNullException>("body", () => ParallelAsync.ForAsync(0, 1, new ParallelOptions(), null!));
    }

    [Fact]
    public async Task ForAsync_CancellationToken()
    {
      var cts = new CancellationTokenSource();
      var loop = 0;
      await Assert.ThrowsAsync<OperationCanceledException>(() => ParallelAsync.ForAsync(1, 3, new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = cts.Token }, i =>
      {
        loop = i;
        if (i == 1)
        {
          cts.Cancel();
        }

        return Task.CompletedTask;
      }));

      Assert.Equal(1, loop);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public async Task ForAsync(int maxDegreeOfParallelism)
    {
      var taskDuration = TimeSpan.FromMilliseconds(100);
      var watch = Stopwatch.StartNew();
      await ParallelAsync.ForAsync(0, 8, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, _ => Task.Delay(taskDuration));
      watch.Stop();

      var expectedDuration = taskDuration.TotalMilliseconds * 8 / maxDegreeOfParallelism;
      Assert.InRange(watch.ElapsedMilliseconds, expectedDuration, expectedDuration * 1.5);
    }

    [Fact]
    public void ForEachAsync_Source_Null()
    {
      Assert.ThrowsAsync<ArgumentNullException>("source", () => ParallelAsync.ForEachAsync<object>(null!, new ParallelOptions(), _ => Task.CompletedTask));
    }
    
    [Fact]
    public void ForEachAsync_ParallelOptions_Null()
    {
      Assert.ThrowsAsync<ArgumentNullException>("parallelOptions", () => ParallelAsync.ForEachAsync(Enumerable.Empty<int>(), null!, _ => Task.CompletedTask));
    }

    [Fact]
    public void ForEachAsync_Body_Null()
    {
      Assert.ThrowsAsync<ArgumentNullException>("body", () => ParallelAsync.ForEachAsync(Enumerable.Empty<int>(), new ParallelOptions(), null!));
    }

    [Fact]
    public async Task ForEachAsync_CancellationToken()
    {
      var cts = new CancellationTokenSource();
      var loop = 0;
      await Assert.ThrowsAsync<OperationCanceledException>(() => ParallelAsync.ForEachAsync(Enumerable.Range(1, 3), new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = cts.Token }, i =>
      {
        loop = i;
        if (i == 1)
        {
          cts.Cancel();
        }

        return Task.CompletedTask;
      }));

      Assert.Equal(1, loop);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public async Task ForEachAsync(int maxDegreeOfParallelism)
    {
      var taskDuration = TimeSpan.FromMilliseconds(100);
      var watch = Stopwatch.StartNew();
      await ParallelAsync.ForEachAsync(Enumerable.Range(0, 8), new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, _ => Task.Delay(taskDuration));
      watch.Stop();

      var expectedDuration = taskDuration.TotalMilliseconds * 8 / maxDegreeOfParallelism;
      Assert.InRange(watch.ElapsedMilliseconds, expectedDuration, expectedDuration * 1.5);
    }
  }
}
