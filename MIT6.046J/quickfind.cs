using System;
using System.Collections.Generic;

public class QuickFind<T> where T : IComparable<T> {
	public List<T> Data { get; }
	private Random Seed {get; }
		
	public QuickFind(List<T> data) {
		Data = data;
		Seed = new Random();
	}
	
	public T Find(int i) {
		if (i < 0 || i >= Data.Count) throw new ArgumentOutOfRangeException(nameof(i));
		List<T> data = new List<T>(Data);
		while (true) {
			int index = Seed.Next(data.Count);
			T pivot = data[index];
			var L = new List<T>();
            var E = new List<T>();
            var G = new List<T>();

            foreach (var v in data) {
                int c = Comparer<T>.Default.Compare(v, pivot);
                if (c < 0) L.Add(v);
                else if (c > 0) G.Add(v);
                else E.Add(v);
            }

            if (Math.Max(L.Count,G.Count) > (3*data.Count)/4) continue;

			if (L.Count > i) {
				data = L;
				continue;
			} else if (E.Count > i - L.Count) {
				return pivot;
			} else {
				data = G;
				i -= E.Count + L.Count;	
			}
		}
	}
}