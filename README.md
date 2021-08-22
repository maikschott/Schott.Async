# Async
A library providing additional task-based APIs.

## `PauseTokenSource` / `PauseToken`
- Allows pausing and resuming methods
- Usage is similar to [CancellationTokenSource](https://docs.microsoft.com/dotnet/api/system.threading.cancellationtokensource) / [CancellationToken](https://docs.microsoft.com/dotnet/api/system.threading.cancellationtoken)
- Based on Stephen Toub: _[Cooperatively pausing async methods](https://devblogs.microsoft.com/pfxteam/cooperatively-pausing-async-methods/)_, but implemented using a ManualResetEventSlim

The following example will print the current time in a task, but will pause and unpause when a key is pressed.
```csharp
async Task Main()
{
  var pauseTokenSource = new PauseTokenSource();
  var task = LongRunningAsync(pauseTokenSource.Token);
	
  while (true)
  {
    Console.ReadKey(false);
    pauseTokenSource.IsPaused = !pauseTokenSource.IsPaused;
    Console.WriteLine(pauseTokenSource.IsPaused ? "Paused" : "Resumed");
  }
	
  await task;
}

async Task LongRunningAsync(PauseToken pauseToken, CancellationToken cancellationToken = default)
{
  while (!cancellationToken.IsCancellationRequested)
  {
    // will proceed if not paused, or if paused will wait until unpaused
    async pauseToken.WaitWhilePausedAsync(cancellationToken);
		
    Console.WriteLine(DateTime.Now);
    await Task.Delay(1000);
  }
}
```

## `SingleThreadedSynchronizationContext`
- Synchronization context where continuations resume on the original thread

This class can be used when asynchronous code needs to run in a single-threaded mode, e.g. if it must run in a specific thread. 
However, in Console applications there usually is no synchronization context which means that continuations are scheduled to "random" threads.
WinForms or WPF applications schedule continuations (if `.ConfigureAwait(false)` is not used) to their UI thread, but it sometimes specific code and all its continuations may need to run in a different thread.

In these cases `SingleThreadedSynchronizationContext` can be used.

```csharp
Task DoWorkAsync()
{
  Debug.WriteLine(Environment.CurrentManagedThreadId);
  
  await Task.Delay(1000);
  
  // will print the same thread id as we are in a now in a single-threaded synchronization context
  Debug.WriteLine(Environment.CurrentManagedThreadId);
}

// Async code and its continuations all run on the calling thread
SingleThreadedSynchronizationContext.RunAsync(DoWorkAsync);

// Async code and its continuations all run on the provided threadpool thread
await Task.Run(() => SingleThreadedSynchronizationContext.RunAsync(DoWorkAsync));

// Async code and its continuations all run on the provided STA thread
var thread = new Thread(SingleThreadedSynchronizationContext.RunAsync(DoWorkAsync).GetAwaiter().GetResult) { IsBackground = true, ApartmentState = ApartmentState.STA };
thread.Start();
thread.Join();
```

## `SynchronizationContextRegion` / `AwaitableSynchronizationContextRegion`
- Wraps [SynchronizationContext.SetSynchronizationContext](https://docs.microsoft.com/dotnet/api/system.threading.synchronizationcontext.setsynchronizationcontext) as IDisposable

```csharp
using (new SynchronizationContextRegion(new DispatcherSynchronizationContext()))
{
  // this code runs with the provided synchronization context
}

// this code runs on the original synchronization context again
```
or
```csharp
await new AwaitableSynchronizationContextRegion(new DispatcherSynchronizationContext());
// this code runs with the provided synchronization context
```

### Disabling synchronization context with `SynchronizationContextRegion.None()`
It's common that asynchronous library methods often configure task continuations with `.ConfigureAwait(false)` to avoid switching back to the orginal synchronization context.

In WinForms or WPF applications this avoids that continuations are executed on the UI thread.

This requires that all awaits are configured with `.ConfigureAwait(false)`, which may be cumbersome for highly asynchronous code and is error-prone as it may be accidently forgotten.

Instead `SynchronizationContextRegion.None` can be used which removes the synchronization context for its code block and all called code. All awaits now behave as if they would have been configured with `.ConfigureAwait(false)`.
After leaving its scope the original synchronization context is restored.

```csharp
public async Task MethodAsync()
{
  using (SynchronizationContextRegion.None())
  {
    await DoSomething1Async();
    await DoSomething1Asyn2();
  }
}
```
or
```csharp
public async Task MethodAsync()
{
  await AwaitableSynchronizationContextRegion.None();
  await DoSomething1Async();
  await DoSomething1Asyn2();
}
```

## `TaskExtensions`
Extension methods:
- `public static void Forget(this Task? task)`: fire and forget about tasks,
- `public Task<bool> ToTask(this WaitHandle waitHandle, TimeSpan? timeout = null, CancellationToken cancellationToken = default)`: convert a WaitHandle to a task,
- `public static Task WithCancellation(this Task task, CancellationToken cancellationToken)`: wrap a task not supporting cancellation inside a cancellable task,
- `public static async Task WithTimeout(this Task task, TimeSpan timeout)`: throw a [TimeOutException](https://docs.microsoft.com/dotnet/api/system.timeoutexception) if a task exceeds a provided timeout.

## `ParallelAsync`
- Allows parallel execution of task-based iterations
- Usage is similar to [Parallel.For](https://docs.microsoft.com/dotnet/api/system.threading.tasks.parallel.for) / [Parallel.ForEach](https://docs.microsoft.com/dotnet/api/system.threading.tasks.parallel.foreach) but supports async delegates
- Based on Stephen Toub: _[Implementing a simple ForEachAsync, part 2](https://devblogs.microsoft.com/pfxteam/implementing-a-simple-foreachasync-part-2/)_

```csharp
await ParallelAsync.ForAsync(0, 10, async index =>
{
  await DoSomethingAsync(index);
});
```
or
```csharp
await ParallelAsync.ForEachAsync(enumerable, async item =>
{
  await DoSomethingAsync(item);
});
```

- Maximum amount of parallel running iterations can be specified
- Cancellation token support
- TaskScheduler can be specified
```csharp
var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken };
await ParallelAsync.ForAsync(0, 10, parallelOptions, async index =>
{
  await DoSomethingAsync(index);
});
```
