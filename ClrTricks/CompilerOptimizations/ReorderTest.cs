using System.Threading;
using System.Threading.Tasks;

namespace ClrTricks.CompilerOptimizations
{
    /// <summary>
    /// In debug mode this sample works as expected.
    /// But built in release the program will dead lock because compiler reorders instructions.
    /// Volatile variable will solve the problem.
    /// Compiler caches _a var in processor's register - Volatile says not to do it
    /// Эти методы гарантируют две вещи:
    /// отсутствие оптимизаций компилятора и отсутствие перестановок инструкций в соответствии с свойставми volatile read или write.
    /// Строго говоря метод VolatileWrite не гарантирует, что значение немедленно станет видимым для других процессоров, а метод VolatileRead не гарантирует, что значение не будет прочитанно из кеша2.
    /// </summary>
    internal static class ReorderTest
    {
        //remove volatile and build in release to see a dead lock
        private static volatile int _a;
        //private static int _a;

        public static void HowVolatileWorks()
        {
            var task = new Task(Bar);
            task.Start();
            Thread.Sleep(1000);
            _a = 0;
            task.Wait();
        }

        private static void Bar()
        {
            _a = 1;
            while (_a == 1)
            {
            }
        }
    }
}
