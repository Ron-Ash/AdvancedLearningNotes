using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
#nullable enable

public abstract class ResultNode<T> where T : IComparable<T> { }
public class NoChangeResultNode<T> : ResultNode<T> where T : IComparable<T> { }
public class ChildResultNode<T> : ResultNode<T> where T : IComparable<T> {
    public Node<T>? Child { get; }
    public ChildResultNode(Node<T>? child) {
        Child = child;
    }
}

public class Node<T>  where T: IComparable<T>{
    public T value  {get; set;}
    public Node<T>? left {get; set;}
	public Node<T>? right {get; set;}
    public bool isLeaf => (left==null) && (right==null);
    public int height { get; private set; }
    public bool balanced { get; private set; }
    public Node(T Value) {
        this.value = Value;
        this.left = null;
		this.right = null;
        this.RecomputeLocal();
    }

    public void RecomputeLocal() {
        height = Math.Max(left?.height ?? 0, right?.height ?? 0) + 1;
        balanced = Math.Abs((left?.height ?? 0)-(right?.height ?? 0)) <= 1;
    }

    public bool TryGetPredecessor(out T result) {
        if (this.left == null) { 
            result = default!; 
            return false;
        }
        result = this.left.Max();
        return true;
    }

    public bool TryGetSuccessor(out T result) {
        if (this.right == null) {
            result = default!; 
            return false;
        }
        result = this.right.Min();
        return true;
    }
	
	public T Min() {
		if (this.left == null) return this.value;
		return this.left!.Min();
	}
	
	public T Max() {
		if (this.right == null) return this.value;
		return this.right!.Max();
	}

    public Node<T>? Search(T Value) {
        int cmp = Comparer<T>.Default.Compare(Value, this.value);
        if (cmp == 0) return this;
		if (!isLeaf) {
			if (cmp < 0) return this.left?.Search(Value);
            else return this.right?.Search(Value); 
        }
		return default(Node<T>?);
    }

    private ResultNode<T> ReBalance(ResultNode<T> result, bool isLeftChild) {
        // reattach child if required
        switch (result) {
            case NoChangeResultNode<T>:
                break;
            case ChildResultNode<T> child:
                if (isLeftChild) this.left = child.Child;
                else this.right = child.Child;
                break;
            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
        this.RecomputeLocal();

        if (this.balanced) return new NoChangeResultNode<T>();
        // given unbalanced tree

        int leftH = this.left?.height ?? 0;
        int rightH = this.right?.height ?? 0;
        bool rotateLeft = leftH > rightH;

        if (rotateLeft) {
            var l = this.left!;
            if ((l.left?.height ?? 0) >= (l.right?.height ?? 0)) {
                var newRight = new Node<T>(this.value) {left=l.right, right=this.right};
                newRight.RecomputeLocal();
                var newRoot = new Node<T>(l.value) {left=l.left, right=newRight};
                newRoot.RecomputeLocal();
                return new ChildResultNode<T>(newRoot);
            } else {
                var lr = l.right!;
                var newLeft = new Node<T>(l.value) {left=l.left, right=lr.left};
                newLeft.RecomputeLocal();
                var newRight = new Node<T>(this.value) {left=lr.right, right=this.right};
                newRight.RecomputeLocal();
                var newRoot = new Node<T>(lr.value) { left=newLeft, right=newRight};
                newRoot.RecomputeLocal();
                return new ChildResultNode<T>(newRoot);
            }
        } else {
            var r = this.right!;
            if ((r.right?.height ?? 0) >= (r.left?.height ?? 0)) {
                var newLeft = new Node<T>(this.value) {right=r.left, left=this.left};
                newLeft.RecomputeLocal();
                var newRoot = new Node<T>(r.value) {right=r.right, left=newLeft};
                newRoot.RecomputeLocal();
                return new ChildResultNode<T>(newRoot);
            } else {
                var rl = r.left!;
                var newRight = new Node<T>(r.value) {right=r.right, left=rl.right};
                newRight.RecomputeLocal();
                var newLeft = new Node<T>(this.value) {right=rl.left, left=this.left};
                newLeft.RecomputeLocal();
                var newRoot = new Node<T>(rl.value) {left=newLeft, right=newRight};
                newRoot.RecomputeLocal();
                return new ChildResultNode<T>(newRoot);
            }
        }
    }

    public ResultNode<T> Insert(T Value) {
        int cmp = Comparer<T>.Default.Compare(Value, this.value);
        
        if (cmp <= 0) {
            if (left == null) { 
                this.left = new Node<T>(Value); 
                this.RecomputeLocal();
                return new NoChangeResultNode<T>();
            }
        } else {
            if (right == null) {
                this.right = new Node<T>(Value);
                this.RecomputeLocal(); 
                return new NoChangeResultNode<T>();
            }
        }
        ResultNode<T> result = cmp <= 0 ? this.left!.Insert(Value) : this.right!.Insert(Value);
        return this.ReBalance(result, cmp<=0);
    }

    public ResultNode<T> Delete(T Value) {
    int cmp = Comparer<T>.Default.Compare(Value, this.value);

    if (cmp == 0) {
        if (!this.TryGetPredecessor(out var replacement)) {
            return new ChildResultNode<T>(this.right);
        }
        this.value = replacement;
        Value = replacement;
        cmp = -1;
    } else if (cmp < 0) {
        if (left == null) return new NoChangeResultNode<T>();
    } else {
        if (right == null) return new NoChangeResultNode<T>();
    }
    ResultNode<T> result = cmp <= 0 ? this.left!.Delete(Value) : this.right!.Delete(Value);
    return this.ReBalance(result, cmp<=0);
}
}

public class AVLTree<T> where T : IComparable<T> {
    public Node<T>? Root { get; private set; }

    public void Insert(T value) {
        if (Root == null) {
            Root = new Node<T>(value); 
            return;
        }
        var result = Root.Insert(value);
        if (result is ChildResultNode<T> child) Root = child.Child;
    }

    public void Delete(T value) {
        if (Root == null) return;
        var result = Root.Delete(value);
        if (result is ChildResultNode<T> child) Root = child.Child; // may become null
    }

    public bool IsBalanced() {
        if (this.Root == null) return true;
        return this.Root!.balanced;
    }

    public List<T> InOrder() {
        var list = new List<T>();
        InOrderRec(Root, list);
        return list;
    }
    private void InOrderRec(Node<T>? node, List<T> list) {
        if (node == null) return;
        InOrderRec(node.left, list);
        list.Add(node.value);
        InOrderRec(node.right, list);
    }
}