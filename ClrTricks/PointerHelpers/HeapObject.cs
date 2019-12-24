using System.Runtime.InteropServices;

namespace ConsoleApp3.PointerHelpers
{
	[StructLayout(LayoutKind.Sequential)]
	class HeapObject
	{
		public int Int;
		public bool Bool;
		public short Short;

		public HeapObject(int _int, bool _bool, short _short)
		{
			Int = _int;
			Bool = _bool;
			Short = _short;
		}
	}
}
