// This file contains all definitions that will need to be implemented, but
// currently just aren't.
// Requirements may change, shifting definitions from _EmptyImplementation.cs
// to _Unimplemented.cs, or vice versa.

using MCMirror.Internal;

// TODO: Note that I'm including the system library when constructing Roslyn. Don't do that anymore.

#pragma warning disable IDE0049 // Name can be simplified
#pragma warning disable IDE0060 // Remove unused parameter
namespace System {

    // This one's gonna be a doozy. O(log(HEAPSIZE)) access, or O(HEAPSIZE)
    // access with the incredibly expensive NBT overhead? Not to mention the
    // complexity of the generated filecount in the former case.
    public abstract class Array { }

    public struct Boolean { }

    // Note: Vanilla c# does some weird shit
    //   public abstract class Enum : ValueType
    // to probably support the myEnum : long {} syntax.
    // I'm not doing that and just using a 32 bit field.
    public struct Enum { }

    // Can I just copy MCMirror.Internal.Primitives.MCInt into here?
    public struct Int32 { }

    public static class Math { }

    // This implementation can only really get started once I have a heap and gc and all that jazz.
    [NoCompile]
    public class Object {
        public virtual String ToString() => throw new CompiletimeNotImplementedException();
        public virtual bool Equals() => throw new CompiletimeNotImplementedException();
        public static bool ReferenceEquals(Object obj1, Object obj2) => throw new CompiletimeNotImplementedException();
        public virtual int GetHashCode() => throw new CompiletimeNotImplementedException();
        public Type GetType() => throw new CompiletimeNotImplementedException();
    }

    public class Random { }

    public struct Single { }
}

namespace System.Collections {
    public interface IEnumerable {
        IEnumerator GetEnumerator();
    }

    public interface IEnumerator {
        bool MoveNext();
        Object Current { get; }
        void Reset();
    }
}

namespace System.Collections.Generic {
    public interface IEnumerable<out T> : IEnumerable {
        new IEnumerator<T> GetEnumerator();
    }

    public interface IEnumerator<out T> : IEnumerator { 
        new T Current { get; }
    }
}