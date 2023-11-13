using System;

namespace MCMirror.Internal {
    /// <summary>
    /// <para>
    /// This exception is thrown at <i>compile-time</i> when in code that
    /// would actually be in the final datapack.
    /// </para>
    /// <para>
    /// More specifically, this means that after the compilation step that
    /// handles this, compilation is stopped when one or more of these
    /// exceptions is encountered in active code. All instances will be listed
    /// of course.
    /// </para>
    /// </summary>
    // TODO: Implement in the compiler
    public class CompiletimeNotImplementedException : Exception {
        public CompiletimeNotImplementedException() { }
#pragma warning disable IDE0060 // Remove unused parameter
        public CompiletimeNotImplementedException(string message) { }
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
