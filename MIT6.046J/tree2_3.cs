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
public class SplitInsertResult<T> : InsertResult<T> where T : IComparable<T> {
    public T Promoted { get; }
    public Node<T> Left { get; }
    public Node<T> Right { get; }
    public SplitInsertResult(T promoted, Node<T> left, Node<T> right) {
        Promoted = promoted;
        Left = left;
        Right = right;
    }
}

public abstract class DeleteResult<T> where T : IComparable<T> { }
public class NoChangeDeleteResult<T> : DeleteResult<T> where T : IComparable<T> { }
public class ChildDeleteResult<T> : DeleteResult<T> where T : IComparable<T> {
    public Node<T> Child { get; }
    public ChildDeleteResult(Node<T> child) {
        Child = child;
    }
}

public abstract class Node<T>  where T: IComparable<T> {
    public bool isLeaf { get; set; } = true;

	public abstract Node<T>? Search(T Value);
	public abstract T Max();
	public abstract T Min();
	public abstract InsertResult<T> Insert(T Value);
    public abstract DeleteResult<T> Delete(T value);
}


public class Node_2<T> : Node<T> where T : IComparable<T> {
	public T value {get;}
	public Node<T>? left;
	public Node<T>? right;
	
	public Node_2(T Value) {
		this.value = Value;
		this.left = null;
		this.right = null;
		this.isLeaf = true;
	}
	
	public override Node<T>? Search(T Value) {
		int comparison = Comparer<T>.Default.Compare(Value, this.value);
		if (comparison == 0) return this;
		if (!isLeaf) {
			if (comparison < 0) return left?.Search(Value);
            else return right?.Search(Value); 
        }
		return null;
	}
	
	public T Predecessor() {
		if (isLeaf) return this.value;
		return left!.Max();
	}
	
	public T Successor() {
		if (isLeaf) return this.value;
		return right!.Min();
	}
	
	public override T Min() {
		if (isLeaf) return this.value;
		return left!.Min();
	}
	
	public override T Max() {
		if (isLeaf) return this.value;
		return right!.Max();
	}
	
    public override InsertResult<T> Insert(T Value) {
        int cmp = Comparer<T>.Default.Compare(Value, this.value);
        if (isLeaf) {
            Node<T> replacement = cmp <= 0
                ? new Node_3<T>(Value, this.value)
                : new Node_3<T>(this.value, Value);
            return new ChildInsertResult<T>(replacement);
        }

        InsertResult<T> result = cmp <= 0
            ? left!.Insert(Value)
            : right!.Insert(Value);
        switch (result) {
            case NoChangeInsertResult<T>:
                break;
            case ChildInsertResult<T> child:
                if (cmp <= 0) this.left = child.Child;
                else this.right = child.Child;
                this.isLeaf = false;
                return new NoChangeInsertResult<T>();
            case SplitInsertResult<T> split:
                int pcmp = Comparer<T>.Default.Compare(split.Promoted, this.value);
                Node_3<T> newNode = pcmp <= 0
                    ? new Node_3<T>(split.Promoted, this.value) { left=split.Left, middle=split.Right, right=this.right, isLeaf=false }
                    : new Node_3<T>(this.value, split.Promoted) { left=this.left, middle=split.Left, right=split.Right, isLeaf=false };
                return new ChildInsertResult<T>(newNode);
            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
        return new NoChangeInsertResult<T>();
    }

    public override DeleteResult<T> Delete(T Value) {
        int cmp = Comparer<T>.Default.Compare(Value, this.value);
        if (isLeaf) {
            return (cmp==0)
                ? new ChildDeleteResult<T>(null) 
                : new NoChangeDeleteResult<T>();
        }

        DeleteResult<T> result = cmp <= 0
            ? left!.Delete(Value)
            : right!.Delete(Value);
        switch (result) {
            case NoChangeDeleteResult<T>:
                break;
            case ChildDeleteResult<T> child:
                break;
            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
        return new NoChangeDeleteResult<T>();
    }
}


public class Node_3<T> : Node<T> where T : IComparable<T> {
	public T leftValue {get;}
	public T rightValue {get;}
	public Node<T>? left;
	public Node<T>? middle;
	public Node<T>? right;	
	public Node_3(T LeftValue, T RightValue) {
		this.leftValue = LeftValue;
		this.rightValue = RightValue;
		this.left = null;
		this.middle = null;
		this.right = null;
		this.isLeaf = true;
	}
	
	public override Node<T>? Search(T Value) {
		int leftComparison = Comparer<T>.Default.Compare(Value, this.leftValue);
		int rightComparison = Comparer<T>.Default.Compare(Value, this.rightValue);
		if (leftComparison == 0 || rightComparison == 0) return this;
		if (!isLeaf) {
			if (leftComparison <= 0) {
				return left!.Search(Value);
			} else if (leftComparison > 0 && rightComparison <= 0) {
				return middle!.Search(Value);
			} else if (rightComparison > 0) {
				return right!.Search(Value);
			} 
		}
		return null;
	}
	
	public T Predecessor() {
		if (isLeaf) return this.leftValue;
		return left!.Max();
	}
	
	public T Successor() {
		if (isLeaf) return this.rightValue;
		return right!.Min();
	}
	
	public override T Min() {
		if (isLeaf) return this.leftValue;
		return left!.Min();
	}
	
	public override T Max() {
		if (isLeaf) return this.rightValue;
		return right!.Max();
	}

    public override InsertResult<T> Insert(T Value) {
        int leftCmp = Comparer<T>.Default.Compare(Value, this.leftValue);
        int rightCmp = Comparer<T>.Default.Compare(Value, this.rightValue);
        if (isLeaf) {
            T median;
            Node_2<T> leftChild, rightChild;
            if (leftCmp<=0) {
                median = this.leftValue;
                leftChild = new Node_2<T>(Value);
                rightChild = new Node_2<T>(this.rightValue);
            } else if (rightCmp<=0) {
                median = Value;
                leftChild = new Node_2<T>(this.leftValue);
                rightChild = new Node_2<T>(this.rightValue);
            } else {
                median = this.rightValue;
                leftChild = new Node_2<T>(this.leftValue);
                rightChild = new Node_2<T>(Value);
            }
            return new SplitInsertResult<T>(median, leftChild, rightChild);
        }

        InsertResult<T> result = leftCmp<=0
            ? left!.Insert(Value)
            : rightCmp<=0
                ? middle!.Insert(Value)
                : right!.Insert(Value);
        switch (result) {
            case NoChangeInsertResult<T>:
                break;
            case ChildInsertResult<T> child:
                if (leftCmp <= 0) this.left = child.Child;
                else if (rightCmp <= 0) this.middle = child.Child;
                else this.right = child.Child;
                this.isLeaf = false;
                return new NoChangeInsertResult<T>();
            case SplitInsertResult<T> split:
                int pcmpLeft = Comparer<T>.Default.Compare(split.Promoted, this.leftValue);
                int pcmpRight = Comparer<T>.Default.Compare(split.Promoted, this.rightValue);
                T median;
                Node_2<T> leftChild, rightChild;
                if (pcmpLeft<=0) {
                    median = this.leftValue;
                    leftChild = new Node_2<T>(split.Promoted) { left=split.Left, right=split.Right, isLeaf=false };
                    rightChild = new Node_2<T>(this.rightValue) { left=this.middle, right=this.right, isLeaf=false };
                } else if (pcmpRight<=0) {
                    median = split.Promoted;
                    leftChild = new Node_2<T>(this.leftValue) { left=this.left, right=split.Left, isLeaf=false };
                    rightChild = new Node_2<T>(this.rightValue) { left=split.Right, right=this.right, isLeaf=false };
                } else {
                    median = this.rightValue;
                    leftChild = new Node_2<T>(this.leftValue) { left=this.left, right=this.middle, isLeaf=false };
                    rightChild = new Node_2<T>(split.Promoted) { left=split.Left, right=split.Right, isLeaf=false };
                }
                return new SplitInsertResult<T>(median, leftChild, rightChild);;
            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
        return new NoChangeInsertResult<T>();
    }

    public override DeleteResult<T> Delete(T Value) {
        return new NoChangeDeleteResult<T>();
    }
}

public class Tree_2_3<T> where T : IComparable<T> {
	public Node<T> root {get; set;}
	
	public Tree_2_3(T value) {
		this.root = new Node_2<T>(value);
	}
	
	public Node<T>? Search(T Value) {
		return this.root.Search(Value);
	}

    public void Insert(T value) {
        var result = root.Insert(value);

        switch (result) {
            case SplitInsertResult<T> split:
                root = new Node_2<T>(split.Promoted) {
                    left = split.Left,
                    right = split.Right,
                    isLeaf = false
                };
                break;
            case ChildInsertResult<T> child:
                root = child.Child;
                break;
            case NoChangeInsertResult<T> _:
                break;
        }
    }
}
public static class TreePrinter {
    public static void Print<T>(Node<T>? node, string indent = "", string prefix="    ") where T : IComparable<T> {
        if (node == null) return;
        if (node is Node_2<T> n2) {
            if (!n2.isLeaf) Print(n2.left, "    "+indent, "┌── ");
            Console.WriteLine($"{indent}{prefix}[{n2.value}]");
            if (!n2.isLeaf) Print(n2.right, "    "+indent, "└── ");
        }
        else if (node is Node_3<T> n3) {
            if (!n3.isLeaf) Print(n3.left, "    "+indent, "┌── ");
            Console.WriteLine($"{indent}{prefix}[{n3.leftValue}");
            if (!n3.isLeaf) Print(n3.middle, "    "+indent, "├── ");
            Console.WriteLine($"{indent}{prefix}{n3.rightValue}]");
            if (!n3.isLeaf) Print(n3.right, "    "+indent, "└── ");
        }
    }
}