using Xunit;

namespace Schott.Async.Tests
{
  public class TaskResultTests
  {
    [Fact]
    public void False()
    {
      var task = TaskResult.False;

      Assert.True(task.IsCompletedSuccessfully);
      Assert.False(task.Result);
      Assert.Same(TaskResult.False, task);
    }

    [Fact]
    public void True()
    {
      var task = TaskResult.True;

      Assert.True(task.IsCompletedSuccessfully);
      Assert.True(task.Result);
      Assert.Same(TaskResult.True, task);
    }

    [Fact]
    public void Default_Int()
    {
      var task = TaskResult<int>.Default;

      Assert.True(task.IsCompletedSuccessfully);
      Assert.Equal(default, task.Result);
      Assert.Same(TaskResult<int>.Default, task);
    }
  }
}