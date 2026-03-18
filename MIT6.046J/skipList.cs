using System;
using System.Collections.Generic;

public class SkipNode<T> where T : IComparable<T> {
	public T Value;
	public SkipNode<T> Next;
	public SkipNode<T> Prev;
	public SkipNode<T> Down;
	public SkipNode<T> Up;
	
	public SkipNode(T value) {
		Value = value;
	}
}

public class SkipList<T> where T : IComparable<T> {
	private List<SkipNode<T>> Heads {get;}
	private Random CoinFlip = new Random();
	private bool Promote => CoinFlip.Next(2)==1;
	
	public SkipList() {
		Heads = new List<SkipNode<T>>();
	}
	
	private List<SkipNode<T>> PathwayDescent(T item) {
		List<SkipNode<T>> pathway = new List<SkipNode<T>>();
		var node = Heads[Heads.Count-1];
		pathway.Add(node);
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
			Heads.Add(new SkipNode<T>(item));
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
			if (i<0) {
				tmp.Next = pathway[i].Next;
				tmp.Prev = node;
				pathway[i].Next = tmp;
				tmp.Down = child;
			} else {
				Heads.Add(tmp);
			}
			child = tmp;
			i--;
		}
	}
	
	public void Delete(T item) {
		List<SkipNode<T>> pathway = PathwayDescent(item);
		var node = pathway[pathway.Count-1];
		if (Comparer<T>.Default.Compare(node.Value, item)!=0) return;
		for (int level=pathway.Count-1; level>=0; level--) {
			if (Comparer<T>.Default.Compare(pathway[level].Value, item)!=0) return;
			node = pathway[level];
			node.Prev.Next = node.Next;
			node.Next.Prev = node.Prev;
		}
	}
}

public class Program
{
	public static void Main()
	{
		Console.WriteLine("Hello World");
	}
}