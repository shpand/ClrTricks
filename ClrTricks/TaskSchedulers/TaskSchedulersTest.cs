using System;
using System.Collections.Generic;
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

			HowLimitedConcurrencyLevelTaskSchedulerWorks();
			HowContinueWithExecuteSynchronouslyWorks();
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

        private static void HowLimitedConcurrencyLevelTaskSchedulerWorks()
        {
            // Create a scheduler that uses two threads. 
            LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(2);
            List<Task> tasks = new List<Task>();

            // Create a TaskFactory and pass it our custom scheduler. 
            TaskFactory factory = new TaskFactory(lcts);
            CancellationTokenSource cts = new CancellationTokenSource();

            // Use our factory to run a set of tasks. 
            Object lockObj = new Object();
            int outputItem = 0;

            for (int tCtr = 0; tCtr <= 4; tCtr++)
            {
                int iteration = tCtr;
                Task t = factory.StartNew(() => {
                    for (int i = 0; i < 1000; i++)
                    {
                        lock (lockObj)
                        {
                            Console.Write("{0} in task t-{1} on thread {2}   ",
                                          i, iteration, Thread.CurrentThread.ManagedThreadId);
                            outputItem++;
                            if (outputItem % 3 == 0)
                                Console.WriteLine();
                        }
                    }
                }, cts.Token);
                tasks.Add(t);
            }
            // Use it to run a second set of tasks.                       
            for (int tCtr = 0; tCtr <= 4; tCtr++)
            {
                int iteration = tCtr;
                Task t1 = factory.StartNew(() => {
                    for (int outer = 0; outer <= 10; outer++)
                    {
                        for (int i = 0x21; i <= 0x7E; i++)
                        {
                            lock (lockObj)
                            {
                                Console.Write("'{0}' in task t1-{1} on thread {2}   ",
                                              Convert.ToChar(i), iteration, Thread.CurrentThread.ManagedThreadId);
                                outputItem++;
                                if (outputItem % 3 == 0)
                                    Console.WriteLine();
                            }
                        }
                    }
                }, cts.Token);
                tasks.Add(t1);
            }

            // Wait for the tasks to complete before displaying a completion message.
            Task.WaitAll(tasks.ToArray());
            cts.Dispose();
            Console.WriteLine("\n\nSuccessful completion.");
        }

        private static void HowContinueWithExecuteSynchronouslyWorks()
        {
            //Example where ContinueWith executes in the same thread
            Console.WriteLine("Main threadId: " + Thread.CurrentThread.ManagedThreadId);
	        var tcs = new TaskCompletionSource<bool>();
	        var cont = tcs.Task.ContinueWith(delegate
	        {
		        Console.WriteLine("ContinueWith threadId: " + Thread.CurrentThread.ManagedThreadId);
	        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously);
	        tcs.SetResult(true);
	        cont.Wait();

	        //Example where ContinueWith executes in different thread because taskScheduler doesn't allow that
            Console.WriteLine("Main threadId: " + Thread.CurrentThread.ManagedThreadId);
	        tcs = new TaskCompletionSource<bool>();
	        cont = tcs.Task.ContinueWith(delegate
	        {
		        Console.WriteLine("ContinueWith threadId: " + Thread.CurrentThread.ManagedThreadId);
	        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, new DummyTaskScheduler());
	        tcs.SetResult(true);
	        cont.Wait();
        }
    }
}
