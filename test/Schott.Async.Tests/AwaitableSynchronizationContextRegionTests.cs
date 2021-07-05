using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Schott.Async.Tests
{
  public class AwaitableSynchronizationContextRegionTests
  {
    [Fact]
    public async Task Await_ChangesSynchronizationContext()
    {
      var oldContext = SynchronizationContext.Current; // XUnit SynchronizationContext

      using var context = new SingleThreadedSynchronizationContext();
      await AwaitSynchronizationContextRegion(context);

      Assert.Same(oldContext, SynchronizationContext.Current);
    }


    [Fact]
    public async Task None_RemovesSynchronizationContext()
    {
      var oldContext = SynchronizationContext.Current; // XUnit SynchronizationContext

      await AwaitSynchronizationContextRegionNone();

      Assert.Same(oldContext, SynchronizationContext.Current);
    }

    private static async Task AwaitSynchronizationContextRegion(SingleThreadedSynchronizationContext context)
    {
      await new AwaitableSynchronizationContextRegion(context);
      Assert.Same(context, SynchronizationContext.Current);
    }

    private static async Task AwaitSynchronizationContextRegionNone()
    {
      await AwaitableSynchronizationContextRegion.None();
      Assert.Null(SynchronizationContext.Current);
    }
  }
}
