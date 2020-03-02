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

        private static async void AsyncVoidTaskThatThrowsException()
        {
            await Task.Delay(1);

            //Exception will be marshaled to QueueSynchronizationContext.Post
            throw new InvalidCastException();
        }
    }
}
