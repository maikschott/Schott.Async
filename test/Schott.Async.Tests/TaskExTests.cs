using System.Threading.Tasks;
using Xunit;

namespace Schott.Async.Tests
{
  public class TaskExTests
  {
    [Theory]
    [InlineData(TaskCreationOptions.LongRunning)]
    public async Task Run(TaskCreationOptions taskCreationOptions)
    {
      var task = TaskEx.Run(() => { }, taskCreationOptions);

      Assert.Equal(taskCreationOptions | TaskCreationOptions.DenyChildAttach, task.CreationOptions);

      await task;
    }

    [Theory]
    [InlineData(TaskCreationOptions.LongRunning)]
    public async Task RunT(TaskCreationOptions taskCreationOptions)
    {
      var task = TaskEx.Run(() => 0, taskCreationOptions);

      Assert.Equal(taskCreationOptions | TaskCreationOptions.DenyChildAttach, task.CreationOptions);

      await task;
    }

    [Theory]
    [InlineData(TaskCreationOptions.LongRunning)]
    public async Task RunTask(TaskCreationOptions taskCreationOptions)
    {
      var task = TaskEx.Run(() => Task.CompletedTask, taskCreationOptions);

      Assert.IsNotType<Task<Task>>(task);

      await task;
    }

    [Theory]
    [InlineData(TaskCreationOptions.LongRunning)]
    public async Task RunTaskT(TaskCreationOptions taskCreationOptions)
    {
      var task = TaskEx.Run(() => Task.FromResult(1), taskCreationOptions);

      Assert.IsNotType<Task<Task<int>>>(task);

      await task;
    }
  }
}