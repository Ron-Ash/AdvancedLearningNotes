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
public class ChildReplaceDeleteResult<T> : DeleteResult<T> where T : IComparable<T> {
    public Node<T> Child { get; }
    public ChildReplaceDeleteResult(Node<T> child) {
        Child = child;
    }
}
public class ChildTransformDeleteResult<T> : DeleteResult<T> where T : IComparable<T> {
    public Node<T> Child { get; }
    public ChildTransformDeleteResult(Node<T> child) {
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
	public T value  {get; set;}
	public Node<T>? left {get; set;}
	public Node<T>? right {get; set;}
    public bool isLeaf => (left==null) && (right==null);
	
	public Node_2(T Value) {
		this.value = Value;
		this.left = null;
		this.right = null;
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
                return new NoChangeInsertResult<T>();
            case SplitInsertResult<T> split:
                int pcmp = Comparer<T>.Default.Compare(split.Promoted, this.value);
                Node_3<T> newNode = pcmp <= 0
                    ? new Node_3<T>(split.Promoted, this.value) { left=split.Left, middle=split.Right, right=this.right}
                    : new Node_3<T>(this.value, split.Promoted) { left=this.left, middle=split.Left, right=split.Right};
                return new ChildInsertResult<T>(newNode);
            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
        return new NoChangeInsertResult<T>();
    }

    public override DeleteResult<T> Delete(T Value) {
        int cmp = Comparer<T>.Default.Compare(Value, this.value);
        if (isLeaf)
            return cmp == 0
                ? new ChildTransformDeleteResult<T>(null)
                : new NoChangeDeleteResult<T>();

        bool goLeft = cmp<0;
        T target = Value;
        if (cmp == 0) {
            T successor = right!.Min();
            this.value = successor;
            target = successor;
            goLeft = false;
        }
        DeleteResult<T> result = goLeft 
            ? left!.Delete(target) 
            : right!.Delete(target);

        switch (result) {
            case NoChangeDeleteResult<T>:
                return result;
            case ChildReplaceDeleteResult<T> child:
                if (goLeft) left  = child.Child;
                else right = child.Child;
                return new NoChangeDeleteResult<T>();

            case ChildTransformDeleteResult<T> child:
                return goLeft
                    ? HandleUnderflowLeft(child.Child)
                    : HandleUnderflowRight(child.Child);
            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
    }
    private DeleteResult<T> HandleUnderflowLeft(Node<T>? child) {
        if (right is Node_3<T> r3) {
            var newLeft = new Node_2<T>(this.value) { left=child, right=r3.left };
            var newRight = new Node_2<T>(r3.rightValue) { left=r3.middle, right=r3.right };
            return new ChildReplaceDeleteResult<T>(
                new Node_2<T>(r3.leftValue) { left=newLeft, right=newRight });
        } else {
            var r2 = (Node_2<T>)right!;
            return new ChildTransformDeleteResult<T>(
                new Node_3<T>(this.value, r2.value) { left=child, middle=r2.left, right=r2.right });
        }
    }
    private DeleteResult<T> HandleUnderflowRight(Node<T>? child) {
        if (left is Node_3<T> l3) {
            var newLeft = new Node_2<T>(l3.leftValue) { left=l3.left,  right=l3.middle };
            var newRight = new Node_2<T>(this.value) { left=l3.right, right=child };
            return new ChildReplaceDeleteResult<T>(
                new Node_2<T>(l3.rightValue) { left=newLeft, right=newRight });
        } else {
            var l2 = (Node_2<T>)left!;
            return new ChildTransformDeleteResult<T>(
                new Node_3<T>(l2.value, this.value) { left=l2.left, middle=l2.right, right=child });
        }
    }
}


public class Node_3<T> : Node<T> where T : IComparable<T> {
	public T leftValue  {get; set;}
	public T rightValue  {get; set;}
	public Node<T>? left {get; set;}
	public Node<T>? middle {get; set;}
	public Node<T>? right {get; set;}	
    public bool isLeaf => (left==null) && (middle==null) && (right==null);

	public Node_3(T LeftValue, T RightValue) {
		this.leftValue = LeftValue;
		this.rightValue = RightValue;
		this.left = null;
		this.middle = null;
		this.right = null;
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
                return new NoChangeInsertResult<T>();
            case SplitInsertResult<T> split:
                int pcmpLeft = Comparer<T>.Default.Compare(split.Promoted, this.leftValue);
                int pcmpRight = Comparer<T>.Default.Compare(split.Promoted, this.rightValue);
                T median;
                Node_2<T> leftChild, rightChild;
                if (pcmpLeft<=0) {
                    median = this.leftValue;
                    leftChild = new Node_2<T>(split.Promoted) { left=split.Left, right=split.Right };
                    rightChild = new Node_2<T>(this.rightValue) { left=this.middle, right=this.right };
                } else if (pcmpRight<=0) {
                    median = split.Promoted;
                    leftChild = new Node_2<T>(this.leftValue) { left=this.left, right=split.Left };
                    rightChild = new Node_2<T>(this.rightValue) { left=split.Right, right=this.right };
                } else {
                    median = this.rightValue;
                    leftChild = new Node_2<T>(this.leftValue) { left=this.left, right=this.middle };
                    rightChild = new Node_2<T>(split.Promoted) { left=split.Left, right=split.Right };
                }
                return new SplitInsertResult<T>(median, leftChild, rightChild);;
            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
        return new NoChangeInsertResult<T>();
    }

    public override DeleteResult<T> Delete(T Value) {
        int leftCmp  = Comparer<T>.Default.Compare(Value, this.leftValue);
        int rightCmp = Comparer<T>.Default.Compare(Value, this.rightValue);

        if (isLeaf) {
            if (leftCmp == 0)
                return new ChildReplaceDeleteResult<T>(new Node_2<T>(this.rightValue));
            if (rightCmp == 0)
                return new ChildReplaceDeleteResult<T>(new Node_2<T>(this.leftValue));
            return new NoChangeDeleteResult<T>();
        }

        bool goLeft = leftCmp<0;
        bool goMiddle = (leftCmp>=0) && (rightCmp<=0);
        T target = Value;
        if (leftCmp == 0) {
            T successor = middle!.Min();
            this.leftValue = successor;
            target = successor;
            goLeft = false;
            goMiddle = true;
        } else if (rightCmp == 0) {
            T successor = right!.Min();
            this.rightValue = successor;
            target = successor;
            goMiddle = false;
        }
        DeleteResult<T> result = goLeft
            ? left!.Delete(target)
            : goMiddle
                ? middle!.Delete(target)
                : right!.Delete(target);

        switch (result) {
            case NoChangeDeleteResult<T>:
                return result;
            case ChildReplaceDeleteResult<T> child:
                if (goLeft) left = child.Child;
                else if (goMiddle) middle = child.Child;
                else right  = child.Child;
                return new NoChangeDeleteResult<T>();
            case ChildTransformDeleteResult<T> child:
                return goLeft
                    ? HandleUnderflowLeft(child.Child)
                    : goMiddle
                        ? HandleUnderflowMiddle(child.Child)
                        : HandleUnderflowRight(child.Child);
            default:
                throw new InvalidOperationException("Unexpected result type.");
        }
    }
    private DeleteResult<T> HandleUnderflowLeft(Node<T>? child) {
        if (middle is Node_3<T> m3) {
            var newLeft = new Node_2<T>(this.leftValue) { left=child, right=m3.left };
            var newMiddle = new Node_2<T>(m3.rightValue) { left=m3.middle, right=m3.right };
            this.left = newLeft;
            this.middle = newMiddle;
            this.leftValue = m3.leftValue;
            return new NoChangeDeleteResult<T>();
        } else {
            var m2 = (Node_2<T>)middle!;
            var merged = new Node_3<T>(this.leftValue, m2.value) { left=child, middle=m2.left, right=m2.right };
            this.left = merged;
            this.middle = null;
            return new ChildReplaceDeleteResult<T>(
                new Node_2<T>(this.rightValue) { left=merged, right=this.right });
        }
    }
    private DeleteResult<T> HandleUnderflowMiddle(Node<T>? child) {
        if (left is Node_3<T> l3) {
            var newLeft = new Node_2<T>(l3.leftValue) { left=l3.left, right=l3.middle };
            var newMiddle = new Node_2<T>(this.leftValue) { left=l3.right, right=child };
            this.left = newLeft;
            this.middle = newMiddle;
            this.leftValue = l3.rightValue;
            return new NoChangeDeleteResult<T>();
        } else if (right is Node_3<T> r3) {
            var newMiddle = new Node_2<T>(this.rightValue) { left=child, right=r3.left };
            var newRight  = new Node_2<T>(r3.rightValue) { left=r3.middle, right=r3.right };
            this.middle = newMiddle;
            this.right = newRight;
            this.rightValue = r3.leftValue;
            return new NoChangeDeleteResult<T>();
        } else {
            var l2 = (Node_2<T>)left!;
            var merged = new Node_3<T>(l2.value, this.leftValue) { left=l2.left, middle=l2.right, right=child };
            return new ChildReplaceDeleteResult<T>(
                new Node_2<T>(this.rightValue) { left=merged, right=this.right });
        }
    }
    private DeleteResult<T> HandleUnderflowRight(Node<T>? child) {
        if (middle is Node_3<T> m3) {
            var newMiddle = new Node_2<T>(m3.leftValue) { left=m3.left, right=m3.middle };
            var newRight = new Node_2<T>(this.rightValue) { left=m3.right, right=child };
            this.middle = newMiddle;
            this.right = newRight;
            this.rightValue = m3.rightValue;
            return new NoChangeDeleteResult<T>();
        } else {
            var m2 = (Node_2<T>)middle!;
            var merged = new Node_3<T>(m2.value, this.rightValue) { left=m2.left, middle=m2.right, right=child };
            return new ChildReplaceDeleteResult<T>(
                new Node_2<T>(this.leftValue) { left=this.left, right=merged });
        }
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
                };
                break;
            case ChildInsertResult<T> child:
                root = child.Child;
                break;
            case NoChangeInsertResult<T> _:
                break;
        }
    }

    public void Delete(T value) {
        var result = root.Delete(value);
        switch (result) {
            case NoChangeDeleteResult<T> _:
                break;
            case ChildReplaceDeleteResult<T> child:
                root = child.Child;
                break;
            case ChildTransformDeleteResult<T> child:
                root = child.Child;
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