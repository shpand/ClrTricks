using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ClrTricks.ConcurrentCollections
{
	internal class ConcurrentLinkedList<T> : IEnumerable<T>
	{
		public class Node
		{
			public Node(T value)
			{
				Value = value;
			}

			public Node Next;
			public T Value;
		}

		private Node _head;
		private Node _tail;

		public void Add(T data)
		{
			var newNode = new Node(data);

			if (null == Interlocked.CompareExchange(ref _tail, newNode, null))
			{
				_head = _tail;
			}
			else
			{
				var oldTail = Interlocked.Exchange(ref _tail, newNode);
				oldTail.Next = newNode;
			}
		}

		public void Clear()
		{
			while (!(_head == null && _tail == null))
			{
				Volatile.Write(ref _head, null);
				Volatile.Write(ref _tail, null);
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			Node node = _head;
			while (node != null)
			{
				yield return node.Value;
				node = node.Next;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
