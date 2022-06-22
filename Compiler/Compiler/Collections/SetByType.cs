using System;
using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler.Collections {
    /// <summary>
    /// This is a dictionary indexed by the type of what is stored. There can
    /// be no two objects of the same type in here.
    /// </summary>
    // (This does not implement any of the obvious collection interfaces just
    //  because of how *weird* it is to have a collection like this.)
    public class SetByType {
        Dictionary<Type,object> set = new();

        /// <summary>
        /// Adds an entry to the set.
        /// There may only be one entry of a type stored in this set; this
        /// method throws when there already is an item of type <typeparamref name="T"/>.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void Add<T>(T item) => set.Add(typeof(T), item);

        /// <summary>
        /// <inheritdoc cref="Add{T}(T)"/>
        /// <para>
        /// Instead of adding this by the item's type known compile-time, this
        /// uses the specific type this was instantiated as instead.
        /// </para>
        /// </summary>
        public void AddByMostDerived(object item) => set.Add(item.GetType(), item);

        /// <summary>
        /// Get an entry from this set. Throws when there are none of type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <exception cref="KeyNotFoundException"></exception>
        public T Get<T>() => (T)set[typeof(T)];

        public int Count => set.Count;
        public void Clear() => set.Clear();
        public void Contains(Type t) => set.ContainsKey(t);
        public void Contains<T>() => Contains(typeof(T));
    }
}
