using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using System.Collections;


namespace Ares.Extensions {
	public static class ExtensionMethods {
		// Credit to Grenade at StackOverflow
		// https://stackoverflow.com/questions/273313/randomize-a-listt/1262619#1262619
		public static void Shuffle<T>(this IList<T> list){
			int n = list.Count;
			while (n > 1){
				int k = (UnityEngine.Random.Range(byte.MinValue, n * (byte.MaxValue / n)) % n);
				n--;
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		public static IEnumerable<T> RandomSample<T>(this IList<T> list, int amount){
			Debug.Assert(amount <= list.Count);

			List<int> remainingIndices = Enumerable.Range(0, list.Count).ToList();
			T[] samples = new T[amount];

			for(int i = 0; i < amount; i++){
				int sampleIndex = Random.Range(0, remainingIndices.Count);

				samples[i] = list[remainingIndices[sampleIndex]];
				remainingIndices.RemoveAt(sampleIndex);
			}

			return samples;
		}

		// Here for .NET 3.5 users
		// Credit to Spender at StackOverflow
		// https://stackoverflow.com/questions/16927845/cs1061-cs0117-system-linq-enumerable-does-not-contain-a-definition-for-zip
		public static IEnumerable<TResult> AresZip<T1, T2, TResult>(this IEnumerable<T1> first, IEnumerable<T2> second, System.Func<T1, T2, TResult> resultSelector){
			Debug.Assert(first != null && second != null, "Cannot zip null arrays.");

			using(var iteratorA = first.GetEnumerator()){
				using(var iteratorB = second.GetEnumerator()){
					while(iteratorA.MoveNext() && iteratorB.MoveNext()){
						yield return resultSelector(iteratorA.Current, iteratorB.Current);
					}
				}
			}
		}

		public static void AddOneTimeListener(this UnityEvent unityEvent, UnityAction action) {
			UnityAction listener = null;

			listener = () => {
				action();
				unityEvent.RemoveListener(listener);
			};

			unityEvent.AddListener(listener);
		}

		public static void AddOneTimeListener<T>(this UnityEvent<T> unityEvent, UnityAction<T> action) {
			UnityAction<T> listener = null;

			listener = p1 => {
				action(p1);
				unityEvent.RemoveListener(listener);
			};

			unityEvent.AddListener(listener);
		}

		public static void AddOneTimeListener<T1, T2>(this UnityEvent<T1, T2> unityEvent, UnityAction<T1, T2> action) {
			UnityAction<T1, T2> listener = null;

			listener = (p1, p2) => {
				action(p1, p2);
				unityEvent.RemoveListener(listener);
			};

			unityEvent.AddListener(listener);
		}

		public static void AddOneTimeListener<T1, T2, T3>(this UnityEvent<T1, T2, T3> unityEvent, UnityAction<T1, T2, T3> action) {
			UnityAction<T1, T2, T3> listener = null;

			listener = (p1, p2, p3) => {
				unityEvent.RemoveListener(listener);
				action(p1, p2, p3);
			};

			unityEvent.AddListener(listener);
		}

		public static void AddOneTimeListener<T1, T2, T3, T4>(this UnityEvent<T1, T2, T3, T4> unityEvent, UnityAction<T1, T2, T3, T4> action) {
			UnityAction<T1, T2, T3, T4> listener = null;

			listener = (p1, p2, p3, p4) => {
				unityEvent.RemoveListener(listener);
				action(p1, p2, p3, p4);
			};

			unityEvent.AddListener(listener);
		}
	}
}