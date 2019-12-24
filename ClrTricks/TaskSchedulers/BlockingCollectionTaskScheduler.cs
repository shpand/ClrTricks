using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClrTricks.TaskSchedulers
{
	/// <summary>
	/// Executes on ONE thread
	/// </summary>
	public sealed class CustomTaskScheduler : TaskScheduler, IDisposable
	{
		public static CustomTaskScheduler Instance = new CustomTaskScheduler();

		public int ManagedThreadId;
		private readonly BlockingCollection<Task> _tasksCollection = new BlockingCollection<Task>();

		private CustomTaskScheduler()
		{
			var mainThread = new Thread(new ThreadStart(Execute));
			ManagedThreadId = mainThread.ManagedThreadId;

			if (!mainThread.IsAlive)
			{
				mainThread.Start();
			}
		}

		private void Execute()
		{
			foreach (var task in _tasksCollection.GetConsumingEnumerable())
			{
				TryExecuteTask(task);
			}
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return _tasksCollection;
		}

		protected override void QueueTask(Task task)
		{
			if (task != null)
				_tasksCollection.Add(task);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}

		public void Dispose()
		{
			_tasksCollection.CompleteAdding();

			_tasksCollection.Dispose();
		}
	}
}
