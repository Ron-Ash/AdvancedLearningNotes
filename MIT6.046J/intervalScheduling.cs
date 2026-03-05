using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
	public static List<(double start, double stop)> BaseIntervalScheduling(List<(double start, double stop)> schedules) {
		if (schedules.Count <= 1) return schedules;
		List<(double start, double stop)> sorted = schedules.OrderBy(x => x.start).ThenBy(x => x.stop).ToList();
		List<(double start, double stop)> largestSubset = new List<(double start, double stop)>();
		int headOfSubset = -1;
		for (int i = 0; i < sorted.Count; i++) {
			List<(double start, double stop)> subset = new List<(double start, double stop)>();
			for (int j = i+1; j < sorted.Count; j++) {
				if (sorted[j].start >= sorted[i].stop) subset.Add(schedules[j]);
			}
			if (subset.Count > largestSubset.Count) {
				headOfSubset = i;
				largestSubset = subset;
			}
		}
		List<(double start, double stop)> result = new List<(double start, double stop)> { sorted[headOfSubset] };
        result.AddRange(BaseIntervalScheduling(largestSubset));
        return result;
	}

	public static List<(double start, double stop)> ImprovedIntervalScheduling(List<(double start, double stop)> schedules) {
		if (schedules.Count <= 1) return schedules;
		List<(double start, double stop)> sorted = schedules.OrderBy(x => x.stop).ThenBy(x => x.start).ToList();
		List<(double start, double stop)> largestSubset = new List<(double start, double stop)>();
		for (int i = 0; i < sorted.Count; i++) {
			if (largestSubset.Count < 1 || sorted[i].start >= largestSubset[largestSubset.Count-1].stop) {
				largestSubset.Add(sorted[i]);
			}
		}
        return largestSubset;
	}
}