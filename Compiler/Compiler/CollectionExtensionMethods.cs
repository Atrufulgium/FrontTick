using System.Collections.Generic;

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
    }
}
