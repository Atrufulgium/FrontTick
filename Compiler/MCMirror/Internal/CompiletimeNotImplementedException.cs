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
    /// <remarks>
    /// <para>
    /// If you get a CS8115, do not <c>=&gt; ..</c> and instead use 
    /// <c>{ .. }</c>.
    /// </para>
    /// <para>
    /// In its current state, make sure anything that constructs this is
    /// tagged with [NoCompile].
    /// </para>
    /// </remarks>
    // TODO: Implement in the compiler
    [NoCompile]
    public class CompiletimeNotImplementedException : Exception {
        public CompiletimeNotImplementedException() { }
#pragma warning disable IDE0060 // Remove unused parameter
        public CompiletimeNotImplementedException(string message) { }
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
