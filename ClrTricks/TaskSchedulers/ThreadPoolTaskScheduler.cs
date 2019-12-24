using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClrTricks.TaskSchedulers
{
    /// <summary>
    /// Non-working Simplified version of standard ThreadPoolTaskScheduler implementation
    /// </summary>
    internal sealed class ThreadPoolTaskScheduler : TaskScheduler
    {
        private static readonly ParameterizedThreadStart _longRunningThreadWork = s => ((Task)s).RunSynchronously();

        internal ThreadPoolTaskScheduler()
        {
            int id = this.Id;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
	        throw new NotImplementedException();
        }

        protected override void QueueTask(Task task)
        {
            if ((task.CreationOptions & TaskCreationOptions.LongRunning) != TaskCreationOptions.None)
            {
	            Thread thread = new Thread(_longRunningThreadWork)
                {
		            IsBackground = true
	            };
	            thread.Start((object)task);
            }
            else
            {
                bool forceGlobal = (uint)(task.CreationOptions & TaskCreationOptions.PreferFairness) > 0U;
                ThreadPool.UnsafeQueueUserWorkItem(state => task.RunSynchronously(), forceGlobal);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
	        task.RunSynchronously(); 

            return true;
        }
    }
}
