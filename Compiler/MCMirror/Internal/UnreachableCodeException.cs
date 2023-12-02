using System;

namespace MCMirror.Internal {
    /// <summary>
    /// <para>
    /// Do not use.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// On of the rewrite rules inside the compiler turns
    /// <code>
    /// if (cond) { .. }
    /// else { .. }
    /// </code>
    /// into something like
    /// <code>
    /// bool temp = cond;
    /// if (temp) { .. }
    /// if (!temp) { .. }
    /// </code>
    /// which obviously maintains semantics.
    /// </para>
    /// <para>
    /// However, c# disagrees, and if both paths returned a value, it will now
    /// complain that there is a code path continuing beyond the two branches
    /// that doesn't return a value.
    /// </para>
    /// <para>
    /// In order to fix this, the rewrite engine introduces this exception at
    /// the end of methods to make c# not disagree. Fronttick considers this a
    /// noop.
    /// </para>
    /// </remarks>
    [CompilerUsesName]
    [NoCompile]
    public class UnreachableCodeException : Exception {
        public UnreachableCodeException() { }

        // Class constructors aren't supported yet, so instead do
        //   `throw UnreachableCodeException.Exception`.
        [CompilerUsesName]
        public static UnreachableCodeException Exception { get; }
    }
}