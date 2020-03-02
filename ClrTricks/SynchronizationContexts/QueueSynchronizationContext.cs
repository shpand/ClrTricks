using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ClrTricks.SynchronizationContexts
{
    public sealed class QueueSynchronizationContext : SynchronizationContext
    {
        private struct ContinuationInformation
        {
            public SendOrPostCallback Continuation;
            public object State;
        }

        /// <summary>
        /// Continuation queue.
        /// </summary>
        private readonly BlockingCollection<ContinuationInformation> queue =
            new BlockingCollection<ContinuationInformation>();

        /// <summary>
        /// This synchronization context bound thread id.
        /// </summary>
        private int targetThreadId;

        /// <summary>
        /// Number of recursive posts.
        /// </summary>
        private int recursiveCount = 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        public QueueSynchronizationContext()
        {
        }

        /// <summary>
        /// Copy instance.
        /// </summary>
        /// <returns>Copied instance.</returns>
        public override SynchronizationContext CreateCopy()
        {
            return new QueueSynchronizationContext();
        }

        /// <summary>
        /// Send continuation into synchronization context.
        /// </summary>
        /// <param name="continuation">Continuation callback delegate.</param>
        /// <param name="state">Continuation argument.</param>
        public override void Send(SendOrPostCallback continuation, object state)
        {
            this.Post(continuation, state);
        }

        /// <summary>
        /// Post continuation into synchronization context.
        /// </summary>
        /// <param name="continuation">Continuation callback delegate.</param>
        /// <param name="state">Continuation argument.</param>
        public override void Post(SendOrPostCallback continuation, object state)
        {
            // If current thread id is target thread id:
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            if (currentThreadId == targetThreadId)
            {
                // HACK: If current thread is already target thread, invoke continuation directly.
                //   But if continuation has invokeing Post/Send recursive, cause stack overflow.
                //   We can fix this problem by simple solution: Continuation invoke every post into queue,
                //   but performance will be lost.
                //   This counter uses post for scattering (each 50 times).
                if (recursiveCount < 50)
                {
                    recursiveCount++;

                    // Invoke continuation on current thread is better performance.
                    continuation(state);

                    recursiveCount--;
                    return;
                }
            }

            // Add continuation information into queue.
            queue.Add(new ContinuationInformation { Continuation = continuation, State = state });
        }

        /// <summary>
        /// Execute message queue.
        /// </summary>
        public void Run()
        {
            Task.Factory.StartNew(() => Run(null));
        }

        /// <summary>
        /// Execute message queue.
        /// </summary>
        /// <param name="task">Completion awaiting task</param>
        private void Run(Task task)
        {
            // Run only target thread.
            targetThreadId = Thread.CurrentThread.ManagedThreadId;

            // Schedule task completion for abort queue consumer.
            task?.ContinueWith(_ => queue.CompleteAdding());

            // Run queue consumer.
            foreach (var continuationInformation in queue.GetConsumingEnumerable())
            {
                // Invoke continuation.
                continuationInformation.Continuation(continuationInformation.State);
            }
        }
    }
}
