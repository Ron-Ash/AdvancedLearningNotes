using System;
using System.Collections.Generic;

public class SkipNode<T> where T : IComparable<T> {
	public T Value;
	public bool IsSentinel;
	public SkipNode<T> Next;
	public SkipNode<T> Prev;
	public SkipNode<T> Down;
	public SkipNode<T> Up;
	
	public SkipNode(T value, bool isSentinel=false) {
		Value = value;
		IsSentinel = isSentinel;
	}
}

public class SkipList<T> where T : IComparable<T> {
	public List<SkipNode<T>> Heads {get;}
	private Random CoinFlip = new Random();
	private bool Promote => CoinFlip.Next(2)==1;
	
	public SkipList() {
		Heads = new List<SkipNode<T>>();
	}
	
	private List<SkipNode<T>> PathwayDescent(T item) {
		List<SkipNode<T>> pathway = new List<SkipNode<T>>();
		var node = Heads[Heads.Count-1];
		for (int level=Heads.Count-1; level>=0; level--) {
			while (node.Next != null && Comparer<T>.Default.Compare(node.Next.Value, item)<=0) {
				node = node.Next;
			}
			pathway.Add(node);
			if (level > 0) node = node.Down;
			else break;
		}
		return pathway;
	}
	
	public T? Search(T item) {
		List<SkipNode<T>> pathway = PathwayDescent(item);
		var node = pathway[pathway.Count-1];
		return Comparer<T>.Default.Compare(node.Value, item)==0 ? node.Value : default(T);
	}
	
	public void Insert(T item) {	
		if (Heads.Count == 0) {
			var head = new SkipNode<T>(default(T));
			Heads.Add(head);
			head.Next = new SkipNode<T>(item);
			return;
		}

		List<SkipNode<T>> pathway = PathwayDescent(item);
		var node = pathway[pathway.Count-1];
		
		var child = new SkipNode<T>(item);
		child.Next = node.Next;
		child.Prev = node;
		node.Next = child;
		
		int i = pathway.Count-2;
		while (Promote) {
			var tmp = new SkipNode<T>(item);
			if (i>=0) {
				tmp.Next = pathway[i].Next;
				tmp.Prev = node;
				pathway[i].Next = tmp;
				tmp.Down = child;
				child.Up = tmp;
			} else {
				var head = new SkipNode<T>(default(T));
				var parent = Heads[Heads.Count-1];
				parent.Up = head;
				head.Down = parent;
				Heads.Add(head);
				
				head.Next = tmp;
				tmp.Prev = head;
				tmp.Down = child;
				child.Up = tmp;
			}
			child = tmp;
			i--;
		}
	}
	
	public void Delete(T item) {
		List<SkipNode<T>> pathway = PathwayDescent(item);

		for (int level=pathway.Count-1; level>=0; level--) {
			var node = pathway[level];
			if (Comparer<T>.Default.Compare(node.Value, item)!=0) return;
			if (node.Prev != null) node.Prev.Next = node.Next;
			if (node.Next != null) node.Next.Prev = node.Prev;
		}
	}
}

public class Program
{
	public static void Main()
	{
		var list = new SkipList<int>();
		int[] inserts = { 10, 0, 5, 7, -1, 3, 2, 2, 6, 8 };
        foreach (var v in inserts) {
            list.Insert(v);
        }

		Console.WriteLine(list.Heads.Count);
		for (int level = list.Heads.Count - 1; level >= 0; level--) {
			var head = list.Heads[level];
			Console.Write($"L{level}: ");

			var tmp = head.Next; // skip the sentinel's value
			while (tmp != null)
			{
				Console.Write($"{tmp.Value} -> ");
				tmp = tmp.Next;
			}

			Console.WriteLine("null");
		}
	}
}