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
	
	public SkipList() {
		Heads = new List<SkipNode<T>>();
	}
	
	public T? Search(T item) {
		var node = Heads[Heads.Count-1];
		for (int level=Heads.Count-1; level>=0; level--) {
			while (node.Next != null && Comparer<T>.Default.Compare(node.Next.Value, item)<0) {
				node = node.Next;
			}
			if (level > 0) node = node.Down;
			else return Comparer<T>.Default.Compare(node.Value, item)==0 ? node.Value: null;
		}
		return null;
	}
	
	public void Insert(T item) {
		
	}
	
	public void Delete(T item) {
		
	}
}

public class Program
{
	public static void Main()
	{
		Console.WriteLine("Hello World");
	}
}