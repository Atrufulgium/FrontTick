using System;
using System.Collections;
using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// Represents a stack that cannot have more than one copy of any element
    /// according to the comparer. This is computationally efficiently achieved
    /// by using both a stack and a hash set.
    /// </summary>
    internal class StackWithoutDuplicates<T> : ICollection, IEnumerable<T>, IReadOnlyCollection<T> {

        Stack<T> stack;
        HashSet<T> hashset;

        public StackWithoutDuplicates() {
            stack = new();
            hashset = new();
        }
        public StackWithoutDuplicates(IEqualityComparer<T> comparer) {
            stack = new();
            hashset = new(comparer);
        }
        public StackWithoutDuplicates(IEnumerable<T> collection) : this() {
            foreach (var t in collection)
                Add(t);
        }
        public StackWithoutDuplicates(IEnumerable<T> collection, IEqualityComparer<T> comparer) : this(comparer) {
            foreach (var t in collection)
                Add(t);
        }
        public StackWithoutDuplicates(int capacity) {
            stack = new(capacity);
            hashset = new(capacity);
        }
        public StackWithoutDuplicates(int capacity, IEqualityComparer<T> comparer) {
            stack = new(capacity);
            hashset = new(capacity, comparer);
        }

        public int Count => stack.Count;
        public bool IsSynchronized => throw new NotSupportedException();
        public object SyncRoot => throw new NotSupportedException();

        public void Add(T item) {
            if (!hashset.Contains(item)) {
                stack.Push(item);
                hashset.Add(item);
            }
        }

        public void Clear() {
            stack.Clear();
            hashset.Clear();
        }

        public int EnsureCapacity(int newCapacity) {
            int newStackCapacity = stack.EnsureCapacity(newCapacity);
            int newHashsetCapacity = hashset.EnsureCapacity(newCapacity);
            if (newStackCapacity < newHashsetCapacity)
                return newStackCapacity;
            return newHashsetCapacity;
        }

        public void Push(T item) => Add(item);
        public T Peek() => stack.Peek();
        public bool TryPeek(out T result) => stack.TryPeek(out result);
        public T Pop() {
            T popped = stack.Pop();
            hashset.Remove(popped);
            return popped;
        }
        public bool TryPop(out T result) {
            if (!stack.TryPop(out result))
                return false;
            hashset.Remove(result);
            return true;
        }

        public bool Contains(T item) => hashset.Contains(item);
        public void CopyTo(Array array, int index) => ((ICollection)stack).CopyTo(array, index);
        public void CopyTo(T[] array, int arrayIndex) => stack.CopyTo(array, arrayIndex);
        IEnumerator IEnumerable.GetEnumerator() => stack.GetEnumerator();
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)stack).GetEnumerator();

        public void AddRange(IEnumerable<T> items) {
            foreach (var item in items)
                Add(item);
        }
    }
}
