using System.Threading;
using Xunit;

namespace Schott.Async.Tests
{
  public class SynchronizationContextRegionTests
  {
    [Fact]
    public void Ctor_ChangesSynchronizationContext()
    {
      var oldContext = SynchronizationContext.Current;

      using var context = new SingleThreadedSynchronizationContext();
      using (new SynchronizationContextRegion(context))
      {
        Assert.Same(context, SynchronizationContext.Current);
      }

      Assert.Same(oldContext, SynchronizationContext.Current);
    }

    [Fact]
    public void None_RemovesSynchronizationContext()
    {
      var oldContext = SynchronizationContext.Current; // XUnit SynchronizationContext

      using (SynchronizationContextRegion.None)
      {
        Assert.Null(SynchronizationContext.Current);
      }

      Assert.Same(oldContext, SynchronizationContext.Current);
    }
  }
}
