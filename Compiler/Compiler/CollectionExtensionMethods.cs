using System;
using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler {
    public static class CollectionExtensionMethods {

        /// <summary>
        /// Pops the top element of <paramref name="stack"/>, and replaces it
        /// with <paramref name="item"/>. It returns the old value. Throws a
        /// <see cref="System.InvalidOperationException"/> if the stack is empty.
        /// </summary>
        public static T ReplaceTop<T>(this Stack<T> stack, T item) {
            T ret = stack.Pop();
            stack.Push(item);
            return ret;
        }

        /// <summary>
        /// Simply returns whether this IEnumerable has any elements.
        /// </summary>
        public static bool IsEmpty<T>(this IEnumerable<T> collection) {
            foreach (var _ in collection)
                return false;
            return true;
        }

        /// <summary>
        /// Returns the same collection, but with every <paramref name="element"/> removed.
        /// </summary>
        public static IEnumerable<T> Skip<T>(this IEnumerable<T> collection, T element) {
            foreach (var ele in collection) {
                if (!ele.Equals(element))
                    yield return ele;
            }
        }

        public static IEnumerable<T> EnumerateCopy<T>(this IEnumerable<T> collection) {
            var copy = collection.ToList();
            return copy;
        }

        public static T PopFirst<T>(this LinkedList<T> list) {
            if (list.Count == 0)
                throw new InvalidOperationException("The linked list is empty.");

            T res = list.First.Value;
            list.RemoveFirst();
            return res;
        }

        public static T PopLast<T>(this LinkedList<T> list) {
            if (list.Count == 0)
                throw new InvalidOperationException("The linked list is empty.");

            T res = list.Last.Value;
            list.RemoveLast();
            return res;
        }
    }
}
