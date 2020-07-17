using System;
using System.Diagnostics;
using System.Linq;

namespace ClrTricks.Performance
{
    public static class ArrayVsLinkedList
    {
        public class Node
        {
            public Node Next;
            public int Value;
        }

        public static void Run()
        {
            var sw = new Stopwatch();
            const int size = 10 * 1000 * 1000;
            var array = GenerateArray(size);
            var linkedList = GenerateLinkedList(size);

            sw.Start();

            var res = array.Sum(); // Linq.Sum works ~10 times slower than foreach iteration because it works with IEnumerable<int>

            var time = sw.ElapsedMilliseconds;
            Console.WriteLine(size + " elements in array were iterated VIA LINQ SUM for (ms): " + time);

            sw.Restart();

            res = 0;
            foreach (int i in array)
            {
                res += i;
            }

            time = sw.ElapsedMilliseconds;
            Console.WriteLine(size + " elements in array were iterated for (ms): " + time);

            sw.Restart();
            res = 0;
            while (linkedList != null)
            {
                res += linkedList.Value;
                linkedList = linkedList.Next;
            }

            time = sw.ElapsedMilliseconds;
            Console.WriteLine(size + " elements in linkedList were iterated for (ms): " + time);
        }

        private static int[] GenerateArray(int size)
        {
            var array = new int[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = i % 10;
            }

            return array;
        }

        private static Node GenerateLinkedList(int size)
        {
            var start = new Node();
            var current = start;
            current.Value = 0;
            for (int i = 0; i < size; i++)
            {
                current.Next = new Node();
                current.Next.Value = i % 10;
                current = current.Next;
            }

            return start;
        }
    }
}
