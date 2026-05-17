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
	private List<SkipNode<T>> Heads {get;}
	public int LevelCount => Heads.Count;
	
	private readonly Random _rng = Random.Shared;
	private const int MaxLevel = 32;
	
	private bool ShouldPromote() => _rng.Next(2) == 1;
	
	public SkipList() {
		Heads = new List<SkipNode<T>>();
		Heads.Add(new SkipNode<T>(default(T), isSentinel: true));
	}

	private List<SkipNode<T>> PathwayDescent(T item) {
		var pathway = new List<SkipNode<T>>(Heads.Count);
		var node = Heads[Heads.Count-1];
		for (int level=Heads.Count-1; level>=0; level--) {
			while (node.Next != null && node.Next.Value.CompareTo(item) <= 0) {
				node = node.Next;
			}
			pathway.Add(node);
			if (level>0) node = node.Down;
		}
		return pathway;
	}
	
	public bool TryGetValue(T item, out T result) {
		var pathway = PathwayDescent(item);
		var node = pathway[pathway.Count-1];
		if (!node.IsSentinel && node.Value.CompareTo(item)==0) {
			result = node.Value;
			return true;
		}
		result = default(T);
		return false;
	}
	
	public bool Contains(T item) => TryGetValue(item, out _);
	
	public void Insert(T item) {
		var pathway = PathwayDescent(item);
		
		var bottomPred = pathway[pathway.Count-1];
		var child = new SkipNode<T>(item) {
			Next = bottomPred.Next,
			Prev = bottomPred};
		if (bottomPred.Next!=null) bottomPred.Next.Prev = child;
		bottomPred.Next = child;

		int pathwayIdx = pathway.Count-2;
		while (Heads.Count<MaxLevel && ShouldPromote()) {
			SkipNode<T> upperPred;
			if (pathwayIdx >= 0) {
				upperPred = pathway[pathwayIdx];
			} else {
				var newHead = new SkipNode<T>(default(T), isSentinel: true);
				var oldTopHead = Heads[Heads.Count - 1];
				newHead.Down = oldTopHead;
				oldTopHead.Up = newHead;
				Heads.Add(newHead);
				upperPred = newHead;
			}
			
			var tower = new SkipNode<T>(item) {
				Next = upperPred.Next,
				Prev = upperPred,
				Down = child};
			if (upperPred.Next!=null) upperPred.Next.Prev = tower;
			upperPred.Next = tower;
			child.Up = tower;
			
			child = tower;
			pathwayIdx--;
		}
	}
	
	public bool Delete(T item) {
		var pathway = PathwayDescent(item);
		var bottomPred = pathway[pathway.Count - 1];

		if (bottomPred.IsSentinel || bottomPred.Value.CompareTo(item)!=0) {
			return false;
		}
		
		var node = bottomPred;
		while (node!=null) {
			if (node.Prev != null) node.Prev.Next = node.Next;
			if (node.Next != null) node.Next.Prev = node.Prev;
			node = node.Up;
		}
		
		while (Heads.Count>1 && Heads[Heads.Count-1].Next==null) {
			var top = Heads[Heads.Count-1];
			if (top.Down!=null) top.Down.Up = null;
			Heads.RemoveAt(Heads.Count-1);
		}
		
		return true;
	}
	
	public void Print() {
		Console.WriteLine($"Levels: {Heads.Count}");
		for (int level = Heads.Count - 1; level >= 0; level--) {
			Console.Write($"L{level}: HEAD");
			var n = Heads[level].Next;
			while (n != null) {
				Console.Write($" -> {n.Value}");
				n = n.Next;
			}
			Console.WriteLine(" -> null");
		}
	}
}