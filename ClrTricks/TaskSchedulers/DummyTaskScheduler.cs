using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClrTricks.TaskSchedulers
{
	public class DummyTaskScheduler : TaskScheduler
	{
		protected override void QueueTask(Task task)
		{
			ThreadPool.QueueUserWorkItem(delegate { TryExecuteTask(task); });
		}

		/// <summary>
		/// This method SHOULD try to execute task synchronously, but our dummy scheduler always ignores that and schedules task asynchronously
		/// </summary>
		/// <param name="task"></param>
		/// <param name="taskWasPreviouslyQueued"></param>
		/// <returns></returns>
		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			//return TryExecuteTask(task);
			return false;
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return null;
		}
	}
}
