using System;
using System.Collections.Generic;
using System.Linq;

public class Schedule {
	public double Start {get;}
	public double Stop {get;}
	public double Weight {get;}
	
	public Schedule(double start, double stop, double weight) {
		Start = start;
		Stop = stop;
		Weight = weight;
	}
}

public class WeightedIntervalScheduling {
	public double Weight {get;}
	public List<Schedule> Schedules {get;}
	public Dictionary<Schedule, List<Schedule>> Cache;
	
	public WeightedIntervalScheduling(List<Schedule> schedules) {
		Cache = new Dictionary<Schedule, List<Schedule>>();
		Schedules = FindOptimalSchedule(schedules, new Schedule(0,0,0));
		Weight = CalculateWeight(Schedules);
	}
	
	public double CalculateWeight(List<Schedule> schedules) {
		double total = 0;
		for (int i = 0; i < schedules.Count; i++) {
			total += schedules[i].Weight;
		}
		return total;
	}
	
	public List<Schedule> FindOptimalSchedule(List<Schedule> schedules, Schedule head) {
		if (schedules.Count < 1) return new List<Schedule>(){head};
		if (Cache.TryGetValue(head, out var cached)) {
            return cached;
		}
		var sorted = schedules.OrderBy(x => x.Stop).ThenBy(x => x.Start).ToList();
		double maxWeight = 0;
		var maxSchedule = new List<Schedule>();
		for (int i = 0; i < sorted.Count; i++) {
			if (sorted[i].Start >= head.Stop) {
				int nextStartIndex = i + 1;
                int count = sorted.Count - nextStartIndex;
                var tail = (count > 0) ? sorted.GetRange(nextStartIndex, count) : new List<Schedule>();

                var subSchedule = FindOptimalSchedule(tail, sorted[i]);
				double subScheduleWeight = CalculateWeight(subSchedule);
				if (subScheduleWeight > maxWeight) {
					maxWeight = subScheduleWeight;
					maxSchedule = subSchedule;
				}
			}
		}
		var result = new List<Schedule>(){head};
        result.AddRange(maxSchedule);
		Cache[head] = result;
		return result;
	}
}