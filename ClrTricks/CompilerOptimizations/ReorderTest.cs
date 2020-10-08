using System;
using System.Collections.Generic;
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
        private static volatile List<int> _a;
        //private static int _a;

        public static void HowVolatileWorks()
        {
            var task = new Task(Bar);
            task.Start();
            Thread.Sleep(1000);
            _a = new List<int>();
            task.Wait();
        }

        private static void Bar()
        {
            _a = null;
            while (_a == null)
            {
            }
        }


        private static List<int> _list = new List<int>();

        public static void HowVolatileWorks2()
        {
            var task = new Task(Bar2);
            task.Start();
            Thread.Sleep(100);

            var copy = new List<int>(_list);
            copy.Add(1);
            Console.WriteLine("Added 1");
            Thread.Sleep(1);
            copy.Add(2);
            Console.WriteLine("Added 2");
            Thread.Sleep(1);
            _list = copy;
            Console.WriteLine("Swap lists");

            task.Wait();
            Console.WriteLine("Ended");
        }

        private static void Bar2()
        {
            while (_list.Count < 2)
            {
                Console.WriteLine("Inside while. _list.Count: " + _list.Count);
            }
        }
    }
}
