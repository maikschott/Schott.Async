using System.Threading.Tasks;

namespace Schott.Async
{
  public static class TaskResult
  {
    /// <summary>
    /// Cached task returning <see langword="false"/>.
    /// </summary>
    public static readonly Task<bool> False = Task.FromResult(false);

    /// <summary>
    /// Cached task returning <see langword="true"/>.
    /// </summary>
    public static readonly Task<bool> True = Task.FromResult(true);
  }

  public static class TaskResult<T>
  {
    /// <summary>
    /// Cached task returning the default value for <typeparamref name="T"/>.
    /// </summary>
    public static readonly Task<T> Default = Task.FromResult<T>(default!);
  }
}