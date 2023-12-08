// This file contains all System-namespace things with intentionally empty (or
// nearly empty) implementation. Naturally, all of these definitions should
// have [NoCompile]. Please justify the "empty implementation" with a comment.

// Requirements may change, shifting definitions from _EmptyImplementation.cs
// to _Unimplemented.cs, or vice versa.

// Any method returning one of the types in this file should
//   { throw new MCMirror.Internal.CompiletimeNotImplementedException(); }
// and leave it at that.

using MCMirror.Internal;
using System.Collections.Generic;

#pragma warning disable IDE0049 // Name can be simplified
#pragma warning disable IDE0060 // Remove unused parameter
namespace System {

    // Attributes in MCFunction will only be visible at compile-time as
    // reflection won't exist and they don't have use beyond that.
    // This also holds for all derived classes.
    [NoCompile]
    public abstract class Attribute { 
        public virtual object TypeId => throw new CompiletimeNotImplementedException();
    }

    [NoCompile]
    public sealed class AttributeUsageAttribute : Attribute {
        public AttributeUsageAttribute(AttributeTargets validOn) => throw new CompiletimeNotImplementedException();
        public AttributeUsageAttribute(AttributeTargets validOn, bool allowMultiple, bool inherited) => throw new CompiletimeNotImplementedException();
        public bool AllowMultiple { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
        public bool Inherited { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
    }

    // There is literally no way to have dynamic function calls other than the
    // absolutely lovely O(log(#METHODS)) lookup table, taking up O(#METHODS)
    // MCFunction files. Yeah, I won't.
    [NoCompile]
    public abstract class Delegate { }

    // As Minecraft has no concept of a "call stack" and emulating it is too
    // expensive, exceptions become a construct that simply stops all execution
    // with a message in chat.
    // For this reason, only have two of the regular Exception constructors,
    // and don't make them do anything (leave that up to the compiler).
    [NoCompile]
    public class Exception {
        public Exception() { }
        public Exception(String message) { }
    }

    // (This one's even empty in vanilla!)
    [NoCompile]
    public sealed class ParamArrayAttribute : Attribute {
        public ParamArrayAttribute() { }
    }

    // Strings are very whack. They are *possible* to implement in MCFunction,
    // but it will become very ugly, and there is not much benefit.
    // Other than that, there are quite some compile-time strings, but those
    // constants work fine and don't *need* a String implementation.
    // The concats are required at compile time for.. reasons?
    [NoCompile]
    public sealed class String {
        public static String Concat(Object obj) => throw new CompiletimeNotImplementedException();
        public static String Concat(Object obj1, Object obj2) => throw new CompiletimeNotImplementedException();
        public static String Concat(Object obj1, Object obj2, Object obj3) => throw new CompiletimeNotImplementedException();
        public static String Concat(Object obj1, Object obj2, Object obj3, Object obj4, __arglist) => throw new CompiletimeNotImplementedException();
        public static String Concat(params Object[] obj) => throw new CompiletimeNotImplementedException();
        public static String Concat<T>(IEnumerable<T> values) => throw new CompiletimeNotImplementedException();
        public static String Concat(IEnumerable<String> values) => throw new CompiletimeNotImplementedException();
        public static String Concat(String str1, String str2) => throw new CompiletimeNotImplementedException();
        public static String Concat(String str1, String str2, String str3) => throw new CompiletimeNotImplementedException();
        public static String Concat(String str1, String str2, String str3, String str4) => throw new CompiletimeNotImplementedException();
        public static String Concat(params String[] values) => throw new CompiletimeNotImplementedException();
    }

    // There is no reflection; compile-time types could probably be handled if
    // *really* necessary (no), but runtime types are a "why would you".
    [NoCompile]
    public abstract class Type { }

    // Vanilla c# uses reflection-based GetHashCode() and Equals() methods.
    // As MCMirror has no reflection, this is impossible.
    // The memory model is also sufficiently different that it may not even
    // make sense, but I haven't thought that out yet.
    // I could just combine the hashcodes of all the eventually-underlying
    // ints of whatever the struct is.
    [NoCompile]
    public abstract class ValueType { }

    // By definition.
    [NoCompile]
    public struct Void { }
}

namespace System.Diagnostics.CodeAnalysis {
    [NoCompile]
    public sealed class SuppressMessageAttribute : Attribute {
        public SuppressMessageAttribute(string category, string checkId) => throw new CompiletimeNotImplementedException();
        public string Scope { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
        public string Target { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
        public string MessageId { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
        public string Justification { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
    }
}

namespace System.Reflection {
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [NoCompile]
    public sealed class AssemblyCompanyAttribute : Attribute {
        public AssemblyCompanyAttribute(string s) => throw new CompiletimeNotImplementedException();
        public string Company { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [NoCompile]
    public sealed class AssemblyFileVersionAttribute : Attribute {
        public AssemblyFileVersionAttribute(string s) => throw new CompiletimeNotImplementedException();
        public string Version { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [NoCompile]
    public sealed class AssemblyInformationalVersionAttribute : Attribute {
        public AssemblyInformationalVersionAttribute(string s) => throw new CompiletimeNotImplementedException();
        public string InformationalVersion { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [NoCompile]
    public sealed class AssemblyProductAttribute : Attribute {
        public AssemblyProductAttribute(string s) => throw new CompiletimeNotImplementedException();
        public string Product { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [NoCompile]
    public sealed class AssemblyTitleAttribute : Attribute {
        public AssemblyTitleAttribute(string s) => throw new CompiletimeNotImplementedException();
        public string Title { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [NoCompile]
    public sealed class AssemblyVersionAttribute : Attribute {
        public AssemblyVersionAttribute(string s) => throw new CompiletimeNotImplementedException();
        public string Version { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [NoCompile]
    public sealed class AssemblyConfigurationAttribute : Attribute {
        public AssemblyConfigurationAttribute(string s) => throw new CompiletimeNotImplementedException();
        public string Configuration { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
    }
}

namespace System.Runtime.CompilerServices {
    // "Reserved by the compiler" yeah that's me.
    [NoCompile]
    internal class IsExternalInit { }
}

namespace System.Runtime.Versioning {
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    [NoCompile]
    public sealed class TargetFrameworkAttribute : Attribute {
        public TargetFrameworkAttribute(string s) => throw new CompiletimeNotImplementedException();
        public string FrameworkDisplayName { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
        public string FrameworkName { get => throw new CompiletimeNotImplementedException(); set => throw new CompiletimeNotImplementedException(); }
    }
}