using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClrTricks.SynchronizationContexts
{
    public static class SynchronizationContextTest
    {
        public static async Task HowSyncContextWorks()
        {
            var sc = new QueueSynchronizationContext();

            SynchronizationContext.SetSynchronizationContext(sc);

            sc.Run();

            AsyncVoidTaskThatThrowsException();

            await Task.Delay(1000);
        }

        public static async Task HowTaskCompletionSourceWorks()
        {
            var sc = new QueueSynchronizationContext();

            SynchronizationContext.SetSynchronizationContext(sc);

            sc.Run();

            var hadSyncCtx = SynchronizationContext.Current != null; // true

            await TaskCompletionSourceSyncOperation();

            var hasSyncCtx = SynchronizationContext.Current != null; // false!!!!!!!!!!!!
            Console.WriteLine(hadSyncCtx != hasSyncCtx ? "Execution ignored SyncContext!!! To fix the problem TaskCompletionSource(RunAsynchronously)" : "not relevant");
        }

        private static async void AsyncVoidTaskThatThrowsException()
        {
            await Task.Delay(1);

            //Exception will be marshaled to QueueSynchronizationContext.Post
            //throw new InvalidCastException();
        }

        private static Task<int> TaskCompletionSourceSyncOperation()
        {
            var tcs = new TaskCompletionSource<int>();
            Task.Delay(1).ContinueWith(t => tcs.SetResult(2)); //switch to background thread (ContinueWith will execute in background)
            return tcs.Task;
        }
    }
}
