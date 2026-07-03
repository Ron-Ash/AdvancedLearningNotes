using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
#nullable enable

public abstract class InsertResult<T> where T : IComparable<T> { }
public class NoChangeInsertResult<T> : InsertResult<T> where T : IComparable<T> { }
public class ChildInsertResult<T> : InsertResult<T> where T : IComparable<T> {
    public Node<T> Child { get; }
    public ChildInsertResult(Node<T> child) {
        Child = child;
    }
}

public class Node<T>  where T: IComparable<T>{
    public T value  {get; set;}
    public Node<T>? left {get; set;}
	public Node<T>? right {get; set;}
    public bool isLeaf => (left==null) && (right==null);
    public int height => Math.Max(left?.height ?? 0, right?.height ?? 0) + 1;
    public bool balanced => Math.Abs((left?.height ?? 0)-(right?.height ?? 0)) <= 1;
    public Node(T Value) {
        this.value = Value;
        this.left = null;
		this.right = null;
    }

    public T? Predecessor() {
		if (this.left == null) return null;
		return this.left!.Max();
	}
	
	public T? Successor() {
		if (this.right == null) return null;
		return this.right!.Min();
	}
	
	public override T Min() {
		if (this.left == null) return this.value;
		return this.left!.Min();
	}
	
	public override T Max() {
		if (this.right == null) return this.value;
		return this.right!.Max();
	}

    public Node<T>? Search(T Value) {
        int cmp = Comparer<T>.Default.Compare(Value, this.value);
        if (cmp == 0) return this;
		if (!isLeaf) {
			if (cmp < 0) return left?.Search(Value);
            else return right?.Search(Value); 
        }
		return null;
    }

    public InsertResult<T> Insert(T Value){
        int cmp = Comparer<T>.Default.Compare(Value, this.value);
        
        if (cmp <= 0) {
            if (left == null) { 
                this.left = new Node<T>(Value); 
                return new NoChangeInsertResult<T>();
            }
        } else {
            if (right == null) {
                this.right = new Node<T>(Value); 
                return new NoChangeInsertResult<T>();
            }
        }
        InsertResult<T> result = cmp <= 0 ? this.left!.Insert(Value) : this.right!.Insert(Value);

        // reattach child if required
        switch (result) {
            case NoChangeInsertResult<T>:
                break;
            case ChildInsertResult<T> child:
                if (cmp <= 0) this.left = child.Child;
                else this.right = child.Child;
                break;
            default:
                throw new InvalidOperationException("Unexpected result type.");
        }

        if (this.balanced) return new NoChangeInsertResult<T>();
        // given unbalanced tree
        if (cmp <= 0) {
            var l = this.left!;
            if ((l.left?.height ?? 0) >= (l.right?.height ?? 0)) {
                return new ChildInsertResult<T>(
                    new Node<T>(l.value) {
                        left=l.left, 
                        right=new Node<T>(this.value) {left=l.right, right=this.right}
                    }
                );
            } else {
                var lr = l.right!;
                return new ChildInsertResult<T>(
                    new Node<T>(lr.value) {
                        left=new Node<T>(l.value) {left=l.left, right=lr.left}, 
                        right=new Node<T>(this.value) {left=lr.right, right=this.right}
                    }
                );
            }
        } else {
            var r = this.right!;
            if ((r.right?.height ?? 0) >= (r.left?.height ?? 0)) {
                return new ChildInsertResult<T>(
                    new Node<T>(r.value) {
                        right=r.right, 
                        left=new Node<T>(this.value) {right=r.left, left=this.left}
                    }
                );
            } else {
                var rl = r.left!;
                return new ChildInsertResult<T>(
                    new Node<T>(rl.value) {
                        right=new Node<T>(r.value) {right=r.right, left=rl.right}, 
                        left=new Node<T>(this.value) {right=rl.left, left=this.left}
                    }
                );
            }
        }
    }
    // delete should be the same as insert in terms of balancing though require predecessor function for replace and delete method
}