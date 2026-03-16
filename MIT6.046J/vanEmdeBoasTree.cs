using System;
using System.Linq;
using System.Collections.Generic;

#nullable enable

public class Veb {
    public int Size { get; }
	public int Level { get; }
    private readonly int ClusterSize;

    public Dictionary<int, Veb> Clusters { get; private set; }
    public Veb? Summary { get; private set; }
    public int? Min { get; private set; }
    public int? Max { get; private set; }
	public int Count { get; private set; }

    public bool IsLeaf => Size <= 0;

    public Veb (int size, int level=0) {
        Size = size;
		Level = level;
        ClusterSize = (int)Math.Pow(2, Math.Pow(2, Size-1));
		Count = 0;
		
		Clusters = new Dictionary<int, Veb>();
		if (!IsLeaf) {
			Summary = new Veb(Size-1, level+1);
		}
    }

    public void Insert(int item) {
		Count += 1;
        if (IsLeaf) {
            if (Min is null && Max is null) {
                Min = Max = item;
            } else {
                if (item < Min) Min = item;
                if (item > Max) Max = item;
            }
            return;
        }
        if (Min is null) {
            Min = Max = item;
            return;
        }
        if (item < Min) (item, Min) = ((int)Min, item);
        if (item > Max) Max = item;

        int high = item / ClusterSize;
        int low = item % ClusterSize;
        // if enters this loop, guranteed O(1) from clusters loop
        if (!Clusters.TryGetValue(high, out var cluster) && Summary is not null) {
			Summary.Insert(high);
			cluster = new Veb(Size-1, Level+1);
			Clusters[high] = cluster;
		}
        Clusters[high].Insert(low);
    }

	public void Delete(int item) {
		if (Count <= 0 || Min is null || item < Min || Max is null || item > Max) return;
		
		if (IsLeaf || Count <= 1) {
        	if (item == Min && item == Max) Min = Max = null;
        	else if (item == Min) Min = Max;
        	else if (item == Max) Max = Min;
			Count = Math.Max(Count - 1, 0);
        	return;
    	}
		
		var high = item / ClusterSize;
        var low = item % ClusterSize;
		if (item == Min) {
			if (Summary is not null && Summary.Min is not null && 
			Clusters.TryGetValue((int)Summary.Min, out var cluster) && cluster.Min is not null) {
				high = (int)Summary.Min;
				low = (int)cluster.Min;
				Min = high*ClusterSize + low;
			} else {
				Max = Min = null;
				Count = Math.Max(Count - 1, 0);
				return;
			}
		}
		if (Clusters.TryGetValue(high, out var clusters)) { 
			clusters.Delete(low);
			Count = Math.Max(Count - 1, 0);
			// if runs, means that Clusters[high].Delete(low) took O(1) time
			if (clusters.Min is null && Summary is not null) {
				Summary.Delete(high);
				Clusters.Remove(high);
			}
		}
        
		if (item == Max) {
			if (Summary is not null && Summary.Max is not null && 
			Clusters.TryGetValue((int)Summary.Max, out var cluster) && cluster.Max is not null) {
				Max = (int)Summary.Max*ClusterSize + (int)cluster.Max;
			} else {
				Max = Min;
			}
		}
	}

    public int? Successor(int item) {
        if (IsLeaf) {
            if (Min is not null && item < Min) return Min;
            else if (Max is not null && item < Max) return Max;
            else return null;
        }
        if (Min is not null && item < Min) return Min;
        if (Max is not null && item >= Max) return null;

        int high = item / ClusterSize;
        int low = item % ClusterSize;
		if (Clusters.TryGetValue(high, out var cluster) && 
		cluster.Max is not null && low < cluster.Max) {
			var offset = cluster.Successor(low);
			return (offset is null) ? null : high * ClusterSize + offset;
		} 
		var next = Summary!.Successor(high);
		if (next is not null && Clusters.TryGetValue(next.Value, out cluster) 
		&& cluster.Min is not null) {
			return next.Value * ClusterSize + cluster.Min;
		}
		return null;
    }

    public int? Predecessor(int item) {
        if (IsLeaf) {
            if (Max is not null && item > Max) return Max;
            else if (Min is not null && item > Min) return Min;
            else return null;
        }
        if (Max is not null && item > Max) return Max;
        if (Min is not null && item <= Min) return null;

        int high = item / ClusterSize;
        int low = item % ClusterSize;
		if (Clusters.TryGetValue(high, out var cluster) 
		&& cluster.Min is not null && low > cluster.Min) {
			var offset = cluster.Predecessor(low);
			return high * ClusterSize + (offset ?? Clusters[high].Min);
		}
		var next = Summary!.Predecessor(high);
		if (next is not null && Clusters.TryGetValue(next.Value, out cluster) 
		&& cluster.Max is not null) {
			return next.Value * ClusterSize + cluster.Max;
		}
		return Min;
    }

    public void Print() {
		string padding = new String('\t', Level);
		Console.WriteLine($"{padding}Min {Min}, Max {Max}, Count {Count}");
		if (!IsLeaf && Summary is not null) {
			Console.WriteLine($"{padding}Summary");
			Summary.Print();
			foreach (int key in Clusters.Keys) {
				Console.WriteLine($"{padding}Cluster{key}");
				Clusters[key].Print();
			}
		}
	}

    public bool Member(int item) {
		var suc = Successor(item-1);
		var pre = Predecessor(item+1);
		return (suc is not null) && (pre is not null) && (suc == pre);
	}
	
	public int? Minimum() {
		return Min;
	}
	
	public int? Maximum() {
		return Max;
	}

	public List<int> ToList() {
		List<int> values = new List<int>();
		var tmp = Min;
		while (tmp is not null) {
			values.Add((int)tmp);
			tmp = Successor((int)tmp);
		}
		return values;
	}
}