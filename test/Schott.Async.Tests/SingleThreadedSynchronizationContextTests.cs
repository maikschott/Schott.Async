using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Schott.Async.Tests
{
  public class SingleThreadedSynchronizationContextTests
  {
    [Fact]
    public void CreateCopy()
    {
      using var syncContext = new SingleThreadedSynchronizationContext();

      Assert.Same(syncContext, syncContext.CreateCopy());
    }    
    
    [Fact]
    public void Send_SameThread()
    {
      using var syncContext = new SingleThreadedSynchronizationContext();

      bool executed = false;
      syncContext.Send(_ => executed = true, null);

      Assert.True(executed);
    }
    
    [Fact]
    public async Task Send_OtherThread()
    {
      using var syncContext = new SingleThreadedSynchronizationContext();
      
      syncContext.OperationStarted();
      bool executed = false;
      var capturedSyncContext = syncContext;
      var sendTask = Task.Run(() => syncContext.Send(_ =>
      {
        executed = true;
        capturedSyncContext.OperationCompleted();
      }, null));
      Assert.False(executed);

      syncContext.ProcessMessages();
      await sendTask;

      Assert.True(executed);
    }

    [Fact]
    public void Post()
    {
      using var syncContext = new SingleThreadedSynchronizationContext();
      
      syncContext.OperationStarted();
      bool executed = false;
      var capturedSyncContext = syncContext;
      syncContext.Post(_ =>
      {
        executed = true;
        capturedSyncContext.OperationCompleted();
      }, null);

      Assert.False(executed);

      syncContext.ProcessMessages();
      Assert.True(executed);
    }

    [Fact]
    public async Task RunAsync_TestThread()
    {
      await TestRunAsync();
    }

    [Fact]
    public async Task RunAsync_NewThreadPoolThread()
    {
      await Task.Run(async () => await TestRunAsync());
    }

    [Fact]
    public void RunAsync_NewThread()
    {
      var thread = new Thread(TestRunAsync().GetAwaiter().GetResult) { IsBackground = true, ApartmentState = ApartmentState.STA };
      thread.Start();
      thread.Join();
    }

    private static async Task TestRunAsync()
    {
      using var _ = SynchronizationContextRegion.None;

      var currentThread = Environment.CurrentManagedThreadId;

      await SingleThreadedSynchronizationContext.RunAsync(async () =>
      {
        Assert.Equal(currentThread, Environment.CurrentManagedThreadId);

        await Task.Delay(10);

        Assert.Equal(currentThread, Environment.CurrentManagedThreadId);
      });
    }
  }
}
