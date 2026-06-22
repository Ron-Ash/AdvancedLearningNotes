using System;
using System.Collections.Generic;
using System.Linq;

// O(n) time O(n) space
public class Program {
	public static double MedianGuess(List<double> data, int groupSize) {
		var medians = new List<double>();
		for (int i = 0; i < data.Count; i += groupSize) {
			int remaining = data.Count - i;
            int size = Math.Min(groupSize, remaining);
            var subset = data.GetRange(i, size);
			subset.Sort();
			medians.Add(subset[subset.Count/2]);
		}
		medians.Sort();
		return medians[medians.Count/2];
	}
	
	public static double MedianFinding(List<double> data, int position) {
		if (data.Count <= 1) return data[0];
		double x = MedianGuess(data, 5);
		List<double> B = data.Where(y => y<x).ToList();
		List<double> C = data.Where(y => y>x).ToList();
        // modified algorithm to include scenarios where non-unique elements exist in lists
		List<double> E = data.Where(y => y==x).ToList();
		
		if (position <= B.Count) return MedianFinding(B, position);
		else if (position <= B.Count+E.Count) return x;
		else return MedianFinding(C, position-(B.Count+E.Count));
	}
}