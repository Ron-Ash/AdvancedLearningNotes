// O(nlogn) time O(n) space
using System;
using System.Collections.Generic;
using System.Linq;

public class DoublyLinkedList<T> {
	public T Data {get;}
	public DoublyLinkedList<T> Prev {get; internal set;}
	public DoublyLinkedList<T> Next {get; internal set;}
	
	public DoublyLinkedList(T data) {
		Data = data;
		Prev = null;
		Next = null;
	}
	
	public void SetPrev(DoublyLinkedList<T>? prev) {
		Prev = prev;
		if (prev != null && prev.Next != this) prev.SetNext(this);
		return;
	}
	
	public void SetNext(DoublyLinkedList<T>? next) {
		Next = next;
		if (next != null && next.Prev != this) next.SetPrev(this);
		return;
	}
	
	public void ReverseOrder() {
		(Next, Prev) = (Prev, Next);
		if (Prev != null) Prev.ReverseOrder();
		return;
	}
	
	public override string ToString() {
        return $"{Data}";
    }

}

public class CircularDoublyLinkedList<T> {
	public DoublyLinkedList<T> Head {get; internal set;}
	public DoublyLinkedList<T> Tail {get; internal set;}
	public int Count {get; internal set;}
	
	public CircularDoublyLinkedList() {
		Head = null;
		Tail = null;
		Count = 0;
	}
	
	internal bool IsFirstItem(DoublyLinkedList<T> item) {
		if (Count <= 1) {
			Head = item;
			Tail = item;
			Head.SetNext(item);
			Head.SetPrev(item);
			return true;
		}
		return false;
	}
	
	public void RemoveFromStart() {
		if (Head == null) return;
		Count--;
		if (Count <= 0) {
			Head = null;
			Tail = null;
			return;
		}
		Head = Head.Next;
		Head.SetPrev(Tail);
		return;
	}
	
	public void RemoveFromEnd() {
		if (Tail == null) return;
		Count--;
		if (Count <= 0) {
			Head = null;
			Tail = null;
			return;
		}
		Tail = Tail.Prev;
		Tail.SetNext(Head);
		return;
	}
	
	public void AddToStart(DoublyLinkedList<T> head) {
		Count++;
		if (IsFirstItem(head)) return;
		(Head, head) = (head, Head);
		Head.SetNext(head);
		Head.SetPrev(Tail);
		return;
	}
	
	public void AddToEnd(DoublyLinkedList<T> tail) {
		Count++;
		if (IsFirstItem(tail)) return;
		(Tail, tail) = (tail, Tail);
		Tail.SetPrev(tail);
		Tail.SetNext(Head);
		if (Head == null) Head = tail;
		return;
	}
	
	public void ReverseOrder() {
		Head.SetPrev(null);
		Tail.SetNext(null);
		Head.ReverseOrder();
		Head.SetNext(Tail);
		Tail.SetPrev(Head);
		(Head, Tail) = (Tail, Head);
		return;
	}
	
	public void AddList(List<T> list) {
		for (int i = 0; i < list.Count; i++) {
			AddToEnd(new DoublyLinkedList<T>(list[i]));
		}
		return;
    }
	
	public void AddDoublyLinkedList(DoublyLinkedList<T> head, DoublyLinkedList<T> tail) {
		Head = head;
		Tail = tail;
		Head.SetPrev(Tail);
		var nextNode = Head;
		var prevNode = Tail;
		do {
			if (prevNode != nextNode.Prev || nextNode != prevNode.Next) {
				throw new InvalidOperationException(
                    "DoublyLinkedList must fully connected (with exception to tail and head)");
			}
			prevNode = nextNode;
       		nextNode = nextNode.Next;
    	} while (nextNode != Head);
		return;
    }
	
	public override string ToString() {
		if (Head == null) return string.Empty;
		var node = Head;
		var printValues = new List<String>();
		do {
        	printValues.Add(node.ToString());
       		node = node.Next;
    	} while (node != Head);
		return string.Join("<-->", printValues);
    }
	
	public List<T> ToList() {
		var values = new List<T>();
		if (Head == null) return values;
		var node = Head;
		do {
        	values.Add(node.Data);
       		node = node.Next;
    	} while (node != Head);
		return values;
    }
}


public class ConvexHull2D {
	public List<(int x, int y)> Coordinates {get; internal set;}
	
	public ConvexHull2D(List<(int x, int y)> coordinates) {
		var sorted = coordinates.OrderBy(o=>o.x).ToList();
		Coordinates = FindConvexHull(sorted, true);
	}

	static double Cross((int x, int y) o, (int x, int y) a, (int x, int y) b) {
		// vec(a) x vec(b) (cross product) is able to determine which line segment is above which
		// more robust than mid-x y-intercepts (can handle vertical/horizontal segments, and detect collinear segment)
		return (double)(a.x-o.x)*(b.y-o.y) - (double)(a.y-o.y)*(b.x-o.x);
	}

	public (DoublyLinkedList<(int x, int y)>, DoublyLinkedList<(int x, int y)>) FindOptimalTangents(
		CircularDoublyLinkedList<(int x, int y)> left, 
		CircularDoublyLinkedList<(int x, int y)> right, 
		Func<double, double, bool> comp) {
		if (left?.Tail == null || right?.Head == null) {
        	throw new InvalidOperationException("Both left and right hulls must be non-empty.");
		}
		double middle = (left.Head.Data.x+right.Head.Data.x)/2.0;
		
		var leftItem = left.Head;
		var rightItem = right.Head;
		if (left.Count <= 1 && right.Count <= 1) return (leftItem, rightItem);
		int i = 0;
		while (true) {
			var newLeftItem = comp(1,-1) ? leftItem.Prev : leftItem.Next;
			var newRightItem = comp(1,-1) ? rightItem.Next : rightItem.Prev;
			var a = leftItem.Data;
			var d = newLeftItem.Data;
			var b = rightItem.Data;
			var c = newRightItem.Data;
			//double yAB = (a.x == b.x) ? double.PositiveInfinity : 
            //    ((double)(b.y-a.y)/(b.x-a.x))*(middle-a.x)+a.y;
			//double yAC = (a.x == c.x) ? double.PositiveInfinity :
            //    ((double)(c.y-a.y)/(c.x-a.x))*(middle-a.x)+a.y;
			//double yDB = (d.x == b.x) ? double.PositiveInfinity : 
            //    ((double)(b.y-d.y)/(b.x-d.x))*(middle-d.x)+d.y;
			//
			//bool moveLeft = comp(yDB, yAB);
			//bool moveRight = comp(yAC, yAB);
			bool moveLeft = comp(Cross(b, a, d), 0);
			bool moveRight = comp(0, Cross(a, b, c));

			if (!moveRight && !moveLeft) break;
			else if (moveRight) rightItem = newRightItem;
			else leftItem = newLeftItem;
			i++;
		}
		return (leftItem, rightItem);
	}
	
	public CircularDoublyLinkedList<(int x, int y)> MergeConvexHullSegments(
		CircularDoublyLinkedList<(int x, int y)> left,
		CircularDoublyLinkedList<(int x, int y)> right,
		bool isParentLeftHandSide) {
		var (upperTangentLeft, upperTangentRight) = FindOptimalTangents(left,right, (x, y) => x > y);
		var (lowerTangentLeft, lowerTangentRight) = FindOptimalTangents(left,right, (x, y) => x < y);
		upperTangentLeft.SetNext(upperTangentRight);
		lowerTangentRight.SetNext(lowerTangentLeft);
		
		Func<double, double, bool> comparator;
		if (isParentLeftHandSide) {
			comparator = (x, y) => x > y;
		} else {
			comparator = (x, y) => x < y;
		}
		
		DoublyLinkedList<(int x, int y)> head = upperTangentLeft;
		DoublyLinkedList<(int x, int y)> node = upperTangentLeft;
		do {
			if (comparator(node.Data.x, head.Data.x)) head = node;
			node = node.Next;
    	} while (node != upperTangentLeft);
		var result = new CircularDoublyLinkedList<(int x, int y)>();
		result.AddDoublyLinkedList(head, head.Prev);
		return result;
	}
	
	
	public List<(int x, int y)> FindConvexHull(List<(int x, int y)> coordinates, bool isParentLeftHandSide) {
		if (coordinates.Count <= 1) return coordinates;
		int halfway = coordinates.Count/2;
		var left = new CircularDoublyLinkedList<(int x, int y)>();
		left.AddList(FindConvexHull(coordinates.GetRange(0,halfway), true));
		var right = new CircularDoublyLinkedList<(int x, int y)>();
		right.AddList(FindConvexHull(coordinates.GetRange(halfway,coordinates.Count-halfway), false));
		var convex = MergeConvexHullSegments(left,right, isParentLeftHandSide);
		return convex.ToList();
	}
}