using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApp3
{
	public class DataGetterWithLock : IEnumerable<string>
	{
		private readonly object _lockObj;
		private List<string> _data = new List<string>() { "1", "2", "3", "4", "5" };

		public DataGetterWithLock(object lockObj)
		{
			_lockObj = lockObj;
		}

		public IEnumerable<string> GetData(string listName)
		{
			Debug.WriteLine("{0} Starts", listName);
			bool hasLock = false;

			try
			{
				Monitor.Enter(_lockObj, ref hasLock);

				Debug.WriteLine("{0} Lock Taken: {1}", listName, hasLock);

				//Lock will not be released until the whole iteration on _data ends!!!!
				//To fix this problem we can wrap this into inner method GetEnumInt()
				foreach (string s in _data)
				{
					yield return s;
				}
			}
			finally
			{
				Monitor.Exit(_lockObj);
				hasLock = false;
				Debug.WriteLine("{0} Lock Released", listName);
			}
		}

		private IEnumerable<string> GetEnumInt()
		{
			foreach (string s in _data)
			{
				yield return s;
			}
		}

		public IEnumerator<string> GetEnumeratorInt()
		{
			foreach (string s in _data)
			{
				Debug.WriteLine("GetEnumeratorInt yield return");
				yield return s;
			}
		}

		public IEnumerator<string> GetEnumerator()
		{
			bool __lockWasTaken = false;
			try
			{
				System.Threading.Monitor.Enter(_lockObj, ref __lockWasTaken);

				Debug.WriteLine("GetEnumerator lock acquired: " + __lockWasTaken);



				return GetEnumeratorInt();
			}
			finally
			{
				Debug.WriteLine("GetEnumerator lock RELEASE!!!!!!!!!!: " + __lockWasTaken);
				if (__lockWasTaken) System.Threading.Monitor.Exit(_lockObj);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumeratorInt();
		}
	}
}
