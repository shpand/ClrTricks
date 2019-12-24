using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClrTricks.TaskSchedulers
{
	public class TaskSchedulersTest
	{
		private static readonly CustomTaskScheduler _taskScheduler = CustomTaskScheduler.Instance;
		public static async Task HowTaskSchedulersWork()
		{
			Console.WriteLine("Because it's a Console app Current SyncContext is: " + (SynchronizationContext.Current?.ToString() ?? "Null"));
			Console.WriteLine("Because SyncContext is NULL TaskScheduler.Current is equal to TaskScheduler.Default: " + (TaskScheduler.Current == TaskScheduler.Default));

			Console.WriteLine("Current ThreadId: " + Thread.CurrentThread.ManagedThreadId);
			Console.WriteLine("CustomTaskScheduler ThreadId: " + _taskScheduler.ManagedThreadId);

			await Task.Factory.StartNew(DoWork, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);

			//Task Run will ALWAYS run on Default scheduler
			await Task.Run(DoWork);

			Console.WriteLine("Finished executing all parent and child tasks on ThreadId: " + Thread.CurrentThread.ManagedThreadId);
		}

		private static void DoWork()
		{
			Console.WriteLine("Executing task on specified TaskScheduler. ThreadId: " + Thread.CurrentThread.ManagedThreadId);

			//TaskScheduler.Current == CustomTaskScheduler.Instance
			Task.Factory.StartNew(DoChildWork, CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskScheduler.Current);

			//This task will be run on TaskScheduler.Default. This Task is not attached to parent (it's default behavior) so parent task may easily get completed before child task even started
			Task.Factory.StartNew(DoChildWork, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}

		private static void DoChildWork()
		{
			Console.WriteLine("Executing child task on ThreadId: " + Thread.CurrentThread.ManagedThreadId);
		}
	}
}
