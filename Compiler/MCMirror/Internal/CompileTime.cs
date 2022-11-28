namespace MCMirror.Internal {
    /// <summary>
    /// A class of methods whose effect is only at compile-time.
    /// </summary>
    /// <remarks>
    /// The method bodies are irrelevant, as their behaviour is implemented in
    /// the compiler somewhere.
    /// </remarks>
    public static class CompileTime {
        /// <summary>
        /// Converts an integer variable into the name the local context uses
        /// in MCFunction to access its value. For instance, a method parameter
        /// turns into <tt>"#namespace:class.method##arg0"</tt>.
        /// </summary>
        /// <param name="variable">
        /// The variable to convert to its name. This *must* be a single
        /// identifier, not a constant, the result of arithmetic, etc.
        /// </param>
        // This is implemented in the "VarNameMethodRewriter" class.
#pragma warning disable IDE0060 // Remove unused parameter
        [CustomCompiled("VarName")]
        public static string VarName(int variable) { return null; }
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
