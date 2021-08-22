using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Schott.Async.Tests
{
  public class TaskExtensionsTests : IDisposable
  {
    private readonly ManualResetEventSlim manualResetEvent;

    public TaskExtensionsTests()
    {
      manualResetEvent = new ManualResetEventSlim();
    }

    [Fact]
    public void Forget()
    {
      Task.Delay(1).Forget();
    }

    [Fact]
    public void Forget_Null()
    {
      ((Task)null).Forget();
    }

    [Fact]
    public void Forget_Completed()
    {
      Task.CompletedTask.Forget();
    }

    [Fact]
    public void Forget_Exception()
    {
      Task.FromException(new InvalidOperationException()).Forget();
    }

    [Fact]
    public async Task ToTask_WaitHandleNull()
    {
      await Assert.ThrowsAsync<ArgumentNullException>("waitHandle", () => ((WaitHandle)null)!.ToTask());
    }

    [Fact]
    public void ToTask_WaitHandleSetBefore()
    {
      manualResetEvent.Set();
      var task = manualResetEvent.WaitHandle.ToTask();

      Assert.True(task.IsCompletedSuccessfully);
      Assert.True(task.Result);
    }

    [Fact]
    public async Task ToTask_WaitHandleSetLater()
    {
      var safeGuardCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

      var task = manualResetEvent.WaitHandle.ToTask(cancellationToken: safeGuardCts.Token);
      var delayTask = Task.Delay(100).ContinueWith(_ => manualResetEvent.Set());

      await Task.WhenAll(task, delayTask);

      Assert.True(task.Result);
    }

    [Fact]
    public void ToTask_TimeoutNow()
    {
      var task = manualResetEvent.WaitHandle.ToTask(TimeSpan.Zero);

      Assert.True(task.IsCompletedSuccessfully);
      Assert.False(task.Result);
    }

    [Fact]
    public async Task ToTask_CancelledBefore()
    {
      var cts = new CancellationTokenSource();
      cts.Cancel();

      var task = manualResetEvent.WaitHandle.ToTask(cancellationToken: cts.Token);

      Assert.True(task.IsCanceled);

      try
      {
        await task;
      }
      catch (OperationCanceledException e)
      {
        Assert.Equal(cts.Token, e.CancellationToken);
        return;
      }

      Assert.True(false, "Operation was not cancelled");
    }

    [Fact]
    public async Task ToTask_CancelledLater()
    {
      var cts = new CancellationTokenSource();

      try
      {
        var task = manualResetEvent.WaitHandle.ToTask(cancellationToken: cts.Token);
        var delayTask = Task.Delay(100).ContinueWith(_ => cts.Cancel());
        Assert.False(task.IsCompleted);

        await Task.WhenAll(task, delayTask);
      }
      catch (OperationCanceledException e)
      {
        Assert.Equal(cts.Token, e.CancellationToken);
        return;
      }

      Assert.True(false, "Operation was not cancelled");
    }

    [Fact]
    public async Task WithCancellation_NonGeneric_Null()
    {
      await Assert.ThrowsAsync<ArgumentNullException>("task", () => ((Task)null)!.WithCancellation(CancellationToken.None));
    }

    [Fact]
    public void WithCancellation_NonGeneric_Uncancellable()
    {
      var originalTask = new TaskCompletionSource().Task;
      var task = originalTask.WithCancellation(CancellationToken.None);

      Assert.Same(task, originalTask);
    }

    [Fact]
    public async Task WithCancellation_NonGeneric_CancelledBefore()
    {
      var cts = new CancellationTokenSource();
      cts.Cancel();

      var task = new TaskCompletionSource().Task.WithCancellation(cts.Token);

      Assert.True(task.IsCanceled);

      try
      {
        await task;
      }
      catch (OperationCanceledException e)
      {
        Assert.Equal(cts.Token, e.CancellationToken);
      }
    }

    [Fact]
    public async Task WithCancellation_NonGeneric_CancelledAfter()
    {
      var cts = new CancellationTokenSource();

      var task = new TaskCompletionSource().Task.WithCancellation(cts.Token);
      var delayTask = Task.Delay(100).ContinueWith(_ => cts.Cancel());

      Assert.False(task.IsCanceled);

      try
      {
        await Task.WhenAll(task, delayTask);
      }
      catch (OperationCanceledException e)
      {
        Assert.Equal(cts.Token, e.CancellationToken);
      }
    }

    [Fact]
    public async Task WithCancellation_NonGeneric_NotCancelled()
    {
      var cts = new CancellationTokenSource();

      var taskCompletionSource = new TaskCompletionSource();
      var task = taskCompletionSource.Task.WithCancellation(cts.Token);
      var delayTask = Task.Delay(100).ContinueWith(_ => taskCompletionSource.SetResult());

      Assert.False(task.IsCompleted);

      await Task.WhenAll(task, delayTask);
    }

    [Fact]
    public async Task WithCancellation_Generic_Null()
    {
      await Assert.ThrowsAsync<ArgumentNullException>("task", () => ((Task<bool>)null)!.WithCancellation(CancellationToken.None));
    }

    [Fact]
    public void WithCancellation_Generic_Uncancellable()
    {
      var originalTask = new TaskCompletionSource<bool>().Task;
      var task = originalTask.WithCancellation(CancellationToken.None);

      Assert.Same(task, originalTask);
    }

    [Fact]
    public async Task WithCancellation_Generic_CancelledBefore()
    {
      var cts = new CancellationTokenSource();
      cts.Cancel();

      var task = new TaskCompletionSource<bool>().Task.WithCancellation(cts.Token);

      Assert.True(task.IsCanceled);

      try
      {
        await task;
      }
      catch (OperationCanceledException e)
      {
        Assert.Equal(cts.Token, e.CancellationToken);
      }
    }

    [Fact]
    public async Task WithCancellation_Generic_CancelledAfter()
    {
      var cts = new CancellationTokenSource();

      var task = new TaskCompletionSource<bool>().Task.WithCancellation(cts.Token);
      var delayTask = Task.Delay(100).ContinueWith(_ => cts.Cancel());

      Assert.False(task.IsCanceled);

      try
      {
        await Task.WhenAll(task, delayTask);
      }
      catch (OperationCanceledException e)
      {
        Assert.Equal(cts.Token, e.CancellationToken);
      }
    }

    [Fact]
    public async Task WithCancellation_Generic_NotCancelled()
    {
      var cts = new CancellationTokenSource();

      var taskCompletionSource = new TaskCompletionSource<bool>();
      var task = taskCompletionSource.Task.WithCancellation(cts.Token);
      var delayTask = Task.Delay(100).ContinueWith(_ => taskCompletionSource.SetResult(true));

      Assert.False(task.IsCompleted);

      await Task.WhenAll(task, delayTask);

      Assert.True(task.Result);
    }

    [Fact]
    public async Task WithTimeout_NonGeneric_Null()
    {
      await Assert.ThrowsAsync<ArgumentNullException>("task", () => ((Task)null)!.WithTimeout(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task WithTimeout_NonGeneric_NoTimeout()
    {
      await Task.CompletedTask.WithTimeout(TimeSpan.FromSeconds(0.1));
    }

    [Fact]
    public async Task WithTimeout_NonGeneric_Timeout()
    {
      await Assert.ThrowsAsync<TimeoutException>(() => new TaskCompletionSource().Task.WithTimeout(TimeSpan.FromSeconds(0.1)));
    }

    [Fact]
    public async Task WithTimeout_Generic_Null()
    {
      await Assert.ThrowsAsync<ArgumentNullException>("task", () => ((Task<bool>)null)!.WithTimeout(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task WithTimeout_Generic_NoTimeout()
    {
      await Task.FromResult(true).WithTimeout(TimeSpan.FromSeconds(0.1));
    }

    [Fact]
    public async Task WithTimeout_Generic_Timeout()
    {
      await Assert.ThrowsAsync<TimeoutException>(() => new TaskCompletionSource<bool>().Task.WithTimeout(TimeSpan.FromSeconds(0.1)));
    }

    [Fact]
    public void SaferWait()
    {
      var currentThread = Environment.CurrentManagedThreadId;
      int taskThread = 0;

      var task = Task.Delay(100).ContinueWith(_ => taskThread = Environment.CurrentManagedThreadId, TaskContinuationOptions.ExecuteSynchronously);
      task.SaferWait();

      Assert.NotEqual(currentThread, taskThread);
    }

    [Fact]
    public void SaferResult()
    {
      var currentThread = Environment.CurrentManagedThreadId;
      int taskThread = 0;

      var result = Task.Delay(100).ContinueWith(_ =>
      {
        taskThread = Environment.CurrentManagedThreadId;
        return 1;
      }, TaskContinuationOptions.ExecuteSynchronously).SaferResult();

      Assert.Equal(1, result);
      Assert.NotEqual(currentThread, taskThread);
    }

    void IDisposable.Dispose() => manualResetEvent.Dispose();
  }
}