using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ClrTricks.CompilerOptimizations;
using ClrTricks.ConcurrentCollections;
using ClrTricks.Performance;
using ClrTricks.SynchronizationContexts;
using ConsoleApp3.Models;
using ConsoleApp3.PointerHelpers;
using FSharpLibrary;

namespace ConsoleApp3
{
    class Program
    {
		static async Task Main(string[] args)
        {
            ArrayVsLinkedList.Run(); //iteration through array is 5-6 times faster than through linkedList!

            ReorderTest.HowVolatileWorks();

			HowConcurrentLinkedListWorks();
            await SynchronizationContextTest.HowSyncContextWorks();
			await ClrTricks.TaskSchedulers.TaskSchedulersTest.HowTaskSchedulersWork();//this method throws error in async void method and crashes app!

			await HowConfigureAwaitWorksWhenCompletesSynchronously();
			HowPointerWorks();
	        HowExceptionRethrowWorks();
	        HowToUseFSharpInDotNet();
			HowToBypassPrivateConstructor();

			//This method deadlocks!
	        HowLockOnYieldWorks();
        }

        static unsafe void HowPointerWorks()
        {
	        ValueTypePointer();
	        HeapObjectPointer();
	        BigArrayPointer();

			void ValueTypePointer()
			{
				int a = 1;
				int b = a << 4;

				byte[] byteArray = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

				GCHandle h = GCHandle.Alloc(byteArray, GCHandleType.Pinned);
				byte* ptr = (byte*)h.AddrOfPinnedObject();

				long address = (long)ptr;
				ptr = ptr + 1;
				byte value = *ptr;
				byte value2 = *(ptr + 2);

				byte t = *ptr++;

				*ptr = 4;
			}

	        void HeapObjectPointer()
	        {
		        var heapObj = new HeapObject(999, true, 137);

		        lock (heapObj)
				{
					IntPtr pointer = default;

					ReferenceHelpers.GetPinnedPtr(heapObj, ptr => pointer = ptr);

					ulong* castPointer = (ulong*)pointer;

					//every object in the heap takes +4/8 bytes to its original size (pointer to Type). Only then actual data begins
					//Question: Where is data that points to SyncBlock?????
					ulong objectTypePointer = *castPointer;

					//actual data
					int intValue = (int)*++castPointer;
					bool boolValue = *((bool*)castPointer + 1);
					short shortValue = *((short*)castPointer + 2);

					//points to the beginning of the managed object header in .NET (points to its run-time type info)
					IntPtr pointerToObjectHeader = Marshal.ReadIntPtr(pointer);
					IntPtr typePointer = typeof(HeapObject).TypeHandle.Value;

					Debug.Assert((IntPtr)objectTypePointer == typePointer);
					Debug.Assert(pointerToObjectHeader == typePointer);
				}
	        }

	        void BigArrayPointer()
	        {
		        var heapObj = new byte[Int32.MaxValue / 2];
		        heapObj[0] = 111;
		        heapObj[1] = 112;

				lock (heapObj)
		        {
			        IntPtr pointer = default;

			        ReferenceHelpers.GetPinnedPtr(heapObj, ptr => pointer = ptr);

			        ulong* castPointer = (ulong*)pointer;

					//every array in the heap takes +16 bytes to its original size (I believe it depends on size and implementation of clr). Only then actual data begins
					ulong unknownDataPointer = *castPointer;

					//actual data
					byte firstValue = *((byte*)castPointer + 16); //111
					byte secondValue = *((byte*)castPointer + 17); //112
					byte thirdValue = *((byte*)castPointer + 18); //0

					IntPtr typePointer = typeof(byte[]).TypeHandle.Value;

					//Dunno why but objectTypePointer doesn't point to Type
					Debug.Assert((IntPtr)unknownDataPointer != typePointer);
		        }
	        }
		}

        private static void HowExceptionRethrowWorks()
        {
	        try
	        {
		        level1();
	        }
	        catch (Exception e)
	        {
				//StackTrace of this exception will be MODIFIED (prolonged to the place where it was caught)
				//level3 <- level2 <- level1 <- HowExceptionRethrowWorks
				Console.WriteLine(e);
	        }

			void level1()
	        {
		        try
		        {
					level2();
		        }
		        catch (Exception e)
		        {
					//StackTrace of this exception will be from a method where it was thrown and till a method where it was caught.
					//level3 <- level2 <- level1
					Console.WriteLine(e);
			        throw;
		        }
	        }

	        void level2()
	        {
		        level3();
			}

			void level3()
			{
				throw null;
			}
		}

		private static async Task HowConfigureAwaitWorksWhenCompletesSynchronously()
		{
			Task ExecuteSync() => Task.FromResult<bool>(true);

			int currentThreadId = Thread.CurrentThread.ManagedThreadId;
			await ExecuteSync().ConfigureAwait(false);
			int afterConfigureThreadId = Thread.CurrentThread.ManagedThreadId;

			//.ConfigureAwait(false) doesn't work when thread is completed synchronously. the thread will be the same (for UI threads it will still be UI thread!!)
			Debug.Assert(currentThreadId == afterConfigureThreadId);
		}

		private static void HowToUseFSharpInDotNet()
		{
			//How? Easy. Just add library
			Say.hello("print me, F#");

			var array = Enumerable.Range(1, 10000).ToArray();
			var squaredArray = Calculator.SimdSquare(array);

			Debug.Assert(squaredArray[1] == 4);
			Debug.Assert(squaredArray[0] == 1);
		}

		private static void HowLockOnYieldWorks()
		{
			object locker = new object();
			IEnumerable<string> myList0 = new DataGetterWithLock(locker).GetData("l1");
			IEnumerable<string> myList1 = new DataGetterWithLock(locker).GetData("l2");
			IEnumerable<string> myList2 = new DataGetterWithLock(locker).GetData("l3");

			Debug.WriteLine("start Getdata");
			// Demonstrate that breaking out of a foreach loop releases the lock
			var t0 = new Thread(() => {
				foreach (var s0 in myList0)
				{
					Debug.WriteLine("List 0 {0}", s0);
					if (s0 == "2") break;
				}
			});
			Debug.WriteLine("start t0");
			t0.Start();
			t0.Join(); // Acts as 'wait for the thread to complete'
			Debug.WriteLine("end t0");

			// t1's foreach loop will start (meaning previous t0's lock was cleared
			var t1 = new Thread(() => {
				foreach (var s1 in myList1)
				{
					Debug.WriteLine("List 1 {0}", s1);
					// Once another thread will wait on the lock while t1's foreach
					// loop is still active a dead-lock will occure.
					var t2 = new Thread(() => {
						foreach (var s2 in myList2)
						{
							Console.WriteLine("List 2 {0}", s2);
						}
					});
					Debug.WriteLine("start t2");
					t2.Start();
					t2.Join();
					Debug.WriteLine("end t2");
				}
			});
			Debug.WriteLine("start t1");
			t1.Start();
			t1.Join();
			Debug.WriteLine("end t1");
			Debug.WriteLine("end GetData");
		}

		private static void HowToBypassPrivateConstructor()
		{
			//Allocates zeroed bytes object bypassing any constructor
			var classWithPrivateConstructor = FormatterServices.GetUninitializedObject(typeof(ClassWithPrivateConstructor)) as ClassWithPrivateConstructor;
			Console.WriteLine(classWithPrivateConstructor.GetState());
		}

		private static void HowConcurrentLinkedListWorks()
		{
			var cList = new ConcurrentLinkedList<int>();

			cList.Add(1);
			cList.Add(2);
			cList.Add(3);
		}
	}
}
