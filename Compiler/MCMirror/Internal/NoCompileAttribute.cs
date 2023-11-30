using System;

namespace MCMirror.Internal {
    /// <summary>
    /// <para>
    /// This attribute signifies a method, class, or struct that is not to be
    /// compiled into mcfunction at all. It is ignored the full compilation
    /// process.
    /// </para>
    /// <para>
    /// This is applicable to methods, or to full classes/structs. In the
    /// latter case, all containig methods, fields, and properties are ignored.
    /// This includes any static content, whether used or not.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
    [CompilerUsesName]
    [NoCompile]
    public class NoCompileAttribute : Attribute { }
}