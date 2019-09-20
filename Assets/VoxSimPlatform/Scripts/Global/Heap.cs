using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VoxSimPlatform {
    namespace Global {
    	public abstract class Heap<T> : IEnumerable<T> {
    		private const int InitialCapacity = 0;
    		private const int GrowFactor = 2;
    		private const int MinGrow = 1;

    		private int _capacity = InitialCapacity;
    		private T[] _heap = new T[InitialCapacity];
    		private int _tail = 0;

    		public int Count {
    			get { return _tail; }
    		}

    		public int Capacity {
    			get { return _capacity; }
    		}

    		protected Comparer<T> Comparer { get; private set; }
    		protected abstract bool Dominates(T x, T y);

    		protected Dictionary<T, int> quickIndexing = new Dictionary<T, int>();

    		protected Heap() : this(Comparer<T>.Default) {
    		}

    		protected Heap(Comparer<T> comparer) : this(Enumerable.Empty<T>(), comparer) {
    		}

    		protected Heap(IEnumerable<T> collection)
    			: this(collection, Comparer<T>.Default) {
    		}

    		protected Heap(IEnumerable<T> collection, Comparer<T> comparer) {
    			if (collection == null) throw new ArgumentNullException("collection");
    			if (comparer == null) throw new ArgumentNullException("comparer");

    			Comparer = comparer;

    			foreach (var item in collection) {
    				if (Count == Capacity)
    					Grow();

    				_heap[_tail++] = item;
    			}

    			for (int i = Parent(_tail - 1); i >= 0; i--)
    				BubbleDown(i);
    		}

    		public bool Has(T item) {
    			return quickIndexing.ContainsKey(item);
    		}

    		public void Add(T item) {
    			// Unique item
    			if (quickIndexing.ContainsKey(item)) {
    				return;
    			}

    			if (Count == Capacity)
    				Grow();

    			_tail++;
    			_heap[_tail - 1] = item;

    			quickIndexing[item] = _tail - 1;

    			BubbleUp(_tail - 1);
    		}

    		public void Update(T item) {
    			if (quickIndexing.ContainsKey(item)) {
    				int index = quickIndexing[item];

    //				Debug.Log (" === Update ==== " + item + ", " + index);
    //				String all = "";
    //				for (int j = 0; j < _tail; j++) {
    //					all += _heap [j] + ", " + quickIndexing[_heap[j]] + " ; ";
    //				}

    				// Either of these would be not necessary
    				BubbleUp(index);
    				BubbleDown(index);
    			}
    			else {
    				// Just add
    				Add(item);
    			}
    		}

    		private void BubbleUp(int i) {
    			if (i == 0 || Dominates(_heap[Parent(i)], _heap[i]))
    				return; //correct domination (or root)

    			Swap(i, Parent(i));
    			BubbleUp(Parent(i));
    		}

    		public T GetMin() {
    			if (Count == 0) throw new InvalidOperationException("Heap is empty");
    			return _heap[0];
    		}

    		public T TakeMin() {
    			if (Count == 0) throw new InvalidOperationException("Heap is empty");
    			T ret = _heap[0];
    			_tail--;
    			Swap(_tail, 0);
    			BubbleDown(0);
    			return ret;
    		}

    		private void BubbleDown(int i) {
    			int dominatingNode = Dominating(i);
    			if (dominatingNode == i) return;
    			Swap(i, dominatingNode);
    			BubbleDown(dominatingNode);
    		}

    		private int Dominating(int i) {
    			int dominatingNode = i;
    			dominatingNode = GetDominating(YoungChild(i), dominatingNode);
    			dominatingNode = GetDominating(OldChild(i), dominatingNode);

    			return dominatingNode;
    		}

    		private int GetDominating(int newNode, int dominatingNode) {
    			if (newNode < _tail && !Dominates(_heap[dominatingNode], _heap[newNode]))
    				return newNode;
    			else
    				return dominatingNode;
    		}

    		private void Swap(int i, int j) {
    			T tmp = _heap[i];
    			_heap[i] = _heap[j];
    			_heap[j] = tmp;

    			quickIndexing[_heap[i]] = i;
    			quickIndexing[_heap[j]] = j;
    		}

    		private static int Parent(int i) {
    			return (i + 1) / 2 - 1;
    		}

    		private static int YoungChild(int i) {
    			return (i + 1) * 2 - 1;
    		}

    		private static int OldChild(int i) {
    			return (i + 1) * 2;
    		}

    		private void Grow() {
    //			Debug.Log ("Grow");
    			int newCapacity = _capacity * GrowFactor + MinGrow;
    			var newHeap = new T[newCapacity];
    			Array.Copy(_heap, newHeap, _capacity);
    			_heap = newHeap;
    			_capacity = newCapacity;
    		}

    		public IEnumerator<T> GetEnumerator() {
    			return _heap.Take(Count).GetEnumerator();
    		}

    		IEnumerator IEnumerable.GetEnumerator() {
    			return GetEnumerator();
    		}
    	}

    	public class MaxHeap<T> : Heap<T> {
    		public MaxHeap()
    			: this(Comparer<T>.Default) {
    		}

    		public MaxHeap(Comparer<T> comparer)
    			: base(comparer) {
    		}

    		public MaxHeap(IEnumerable<T> collection, Comparer<T> comparer)
    			: base(collection, comparer) {
    		}

    		public MaxHeap(IEnumerable<T> collection) : base(collection) {
    		}

    		protected override bool Dominates(T x, T y) {
    			return Comparer.Compare(x, y) >= 0;
    		}
    	}

    	public class MinHeap<T> : Heap<T> {
    		public MinHeap()
    			: this(Comparer<T>.Default) {
    		}

    		public MinHeap(Comparer<T> comparer)
    			: base(comparer) {
    		}

    		public MinHeap(IEnumerable<T> collection) : base(collection) {
    		}

    		public MinHeap(IEnumerable<T> collection, Comparer<T> comparer)
    			: base(collection, comparer) {
    		}

    		protected override bool Dominates(T x, T y) {
    			return Comparer.Compare(x, y) <= 0;
    		}
    	}
    }
}