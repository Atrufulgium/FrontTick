using System;

namespace MCMirror.Internal {
    /// <summary>
    /// <para>
    /// This attribute signifies a method, class, or struct that is not to be
    /// compiled into mcfunction at all -- as if erased from the code.
    /// </para>
    /// <para>
    /// This is applicable to methods, or to full classes/structs. In the
    /// latter case, all containig methods, fields, and properties are ignored.
    /// This includes any static content, whether used or not.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// In practice this is implemented by running the Visitor
    /// <tt>ApplyNoCompileAttributeRewriter</tt>, so make sure to include that
    /// in your compilation process somewhere in the beginning.
    /// </para>
    /// <para>
    /// It depends on the ordening of your compilation process, but this is
    /// supposed to have precedence over both
    /// <see cref="MCFunctionAttribute"/> and <see cref="CustomCompiledAttribute"/>.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    [NoCompile]
    public class NoCompileAttribute : Attribute { }
}