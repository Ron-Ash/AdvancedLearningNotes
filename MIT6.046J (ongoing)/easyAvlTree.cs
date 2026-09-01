using System;
using System.Collections.Generic;
using System.Linq;
#nullable enable

public abstract class ResultNode<T, TSelf> where T : IComparable<T> where TSelf : Node<T, TSelf> { }
public class NoChangeResultNode<T, TSelf> : ResultNode<T, TSelf> where T : IComparable<T> where TSelf : Node<T, TSelf> { }
public class ChildResultNode<T, TSelf> : ResultNode<T, TSelf> where T : IComparable<T> where TSelf : Node<T, TSelf> {
    public TSelf? Child { get; }
    public ChildResultNode(TSelf? child) {
        Child = child;
    }
}

public class Node<T, TSelf>  where T: IComparable<T> where TSelf: Node<T, TSelf>{
    public T value  {get; set;}
    public TSelf? left {get; set;}
	public TSelf? right {get; set;}
    public bool isLeaf => (left==null) && (right==null);
    public int height { get; private set; }
    public bool balanced { get; private set; }
    public Node(T Value) {
        this.value = Value;
        this.left = null;
		this.right = null;
        // this.RecomputeLocal(); called by child in own constructor
    }

    protected virtual TSelf CreateNode(T Value) {
        throw new NotImplementedException();
    }

    public virtual void RecomputeLocal() {
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

    public TSelf? Search(T Value) {
        int cmp = Comparer<T>.Default.Compare(Value, this.value);
        if (cmp == 0) return (TSelf)this;
		if (!isLeaf) {
			if (cmp < 0) return this.left?.Search(Value);
            else return this.right?.Search(Value); 
        }
		return null;
    }

    private ResultNode<T, TSelf> ReBalance(ResultNode<T, TSelf> result, bool isLeftChild) {
        // reattach child if required
        switch (result) {
            case NoChangeResultNode<T, TSelf>:
                break;
            case ChildResultNode<T, TSelf> child:
                if (isLeftChild) this.left = child.Child;
                else this.right = child.Child;
                break;
            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
        this.RecomputeLocal();

        if (this.balanced) return new NoChangeResultNode<T, TSelf>();
        // given unbalanced tree

        int leftH = this.left?.height ?? 0;
        int rightH = this.right?.height ?? 0;
        bool rotateLeft = leftH > rightH;

        TSelf newRoot;
        if (rotateLeft) {
            var l = this.left!;
            if ((l.left?.height ?? 0) >= (l.right?.height ?? 0)) {
                var newRight = CreateNode(this.value);
                newRight.left=l.right;
                newRight.right=this.right;
                newRight.RecomputeLocal();
                newRoot = CreateNode(l.value);
                newRoot.left=l.left;
                newRoot.right=newRight;
            } else {
                var lr = l.right!;
                var newLeft = CreateNode(l.value);
                newLeft.left=l.left;
                newLeft.right=lr.left;
                newLeft.RecomputeLocal();
                var newRight = CreateNode(this.value);
                newRight.left=lr.right;
                newRight.right=this.right;
                newRight.RecomputeLocal();
                newRoot = CreateNode(lr.value);
                newRoot.left=newLeft;
                newRoot.right=newRight;
            }
        } else {
            var r = this.right!;
            if ((r.right?.height ?? 0) >= (r.left?.height ?? 0)) {
                var newLeft = CreateNode(this.value);
                newLeft.right=r.left;
                newLeft.left=this.left;
                newLeft.RecomputeLocal();
                newRoot = CreateNode(r.value);
                newRoot.right=r.right;
                newRoot.left=newLeft;
            } else {
                var rl = r.left!;
                var newRight = CreateNode(r.value);
                newRight.right=r.right;
                newRight.left=rl.right;
                newRight.RecomputeLocal();
                var newLeft = CreateNode(this.value);
                newLeft.right=rl.left;
                newLeft.left=this.left;
                newLeft.RecomputeLocal();
                newRoot = CreateNode(rl.value);
                newRoot.left=newLeft;
                newRoot.right=newRight;
            }
        }
        newRoot.RecomputeLocal();
        return new ChildResultNode<T, TSelf>(newRoot);
    }

    public ResultNode<T, TSelf> Insert(T Value) {
        int cmp = Comparer<T>.Default.Compare(Value, this.value);
        
        if (cmp <= 0) {
            if (left == null) { 
                this.left = CreateNode(Value); 
                this.RecomputeLocal();
                return new NoChangeResultNode<T, TSelf>();
            }
        } else {
            if (right == null) {
                this.right = CreateNode(Value);
                this.RecomputeLocal(); 
                return new NoChangeResultNode<T, TSelf>();
            }
        }
        ResultNode<T, TSelf> result = cmp <= 0 ? this.left!.Insert(Value) : this.right!.Insert(Value);
        return this.ReBalance(result, cmp<=0);
    }

    public ResultNode<T, TSelf> Delete(T Value) {
        int cmp = Comparer<T>.Default.Compare(Value, this.value);

        if (cmp == 0) {
            if (!this.TryGetPredecessor(out var replacement)) {
                return new ChildResultNode<T, TSelf>(this.right);
            }
            this.value = replacement;
            Value = replacement;
            cmp = -1;
        } else if (cmp < 0) {
            if (left == null) return new NoChangeResultNode<T, TSelf>();
        } else {
            if (right == null) return new NoChangeResultNode<T, TSelf>();
        }
        ResultNode<T, TSelf> result = cmp <= 0 ? this.left!.Delete(Value) : this.right!.Delete(Value);
        return this.ReBalance(result, cmp<=0);
    }
}

public class EasyNode<T,K> : Node<T, EasyNode<T, K>> where T: IComparable<T> {
    public K augmentationValue {get; private set;}
    private K augmentationDefaultValue;
    private Func<K, K, T, K> augmentationCompute;
    public EasyNode(T Value, Func<K,K,T,K> augmentationCompute, K augmentationDefaultValue) : base(Value) {
        this.augmentationDefaultValue = augmentationDefaultValue;
        this.augmentationCompute = augmentationCompute;
        this.RecomputeLocal();
    }

    protected override EasyNode<T,K> CreateNode(T Value) {
        return new EasyNode<T,K>(Value, this.augmentationCompute, this.augmentationDefaultValue);
    }

    public override void RecomputeLocal() {
        base.RecomputeLocal();
        K leftAugmentation = (left != null) ? left.augmentationValue : this.augmentationDefaultValue;
        K rightAugmentation = (right != null) ? right.augmentationValue : this.augmentationDefaultValue;
        this.augmentationValue = this.augmentationCompute(leftAugmentation, rightAugmentation, this.value);
    }
}

public class EasyAVLTree<T, K> where T : IComparable<T> {
    public EasyNode<T, K>? Root { get; private set; }
    private K augmentationDefaultValue;
    private Func<K, K, T, K> augmentationCompute;

    public EasyAVLTree(Func<K, K, T, K> augmentationCompute, K augmentationDefaultValue) {
        this.augmentationCompute = augmentationCompute;
        this.augmentationDefaultValue = augmentationDefaultValue;
    }

    public void Insert(T value) {
        if (Root == null) {
            Root = new EasyNode<T, K>(value, this.augmentationCompute, this.augmentationDefaultValue);
            return;
        }
        var result = Root.Insert(value);
        if (result is ChildResultNode<T, EasyNode<T, K>> child) Root = child.Child;
    }

    public void Delete(T value) {
        if (Root == null) return;
        var result = Root.Delete(value);
        if (result is ChildResultNode<T, EasyNode<T, K>> child) Root = child.Child; // may become null
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
    
    private void InOrderRec(EasyNode<T, K>? node, List<T> list) {
        if (node == null) return;
        InOrderRec(node.left, list);
        list.Add(node.value);
        InOrderRec(node.right, list);
    }
}