namespace MCMirror.Internal {
    /// <summary>
    /// A class of methods whose effect is only at compile-time.
    /// </summary>
    public static class CompileTime {
        /// <summary>
        /// <para>
        /// Converts an integer variable into the name the local context uses
        /// in MCFunction to access its value. For instance, a method parameter
        /// turns into <tt>"#namespace:class.method##arg0"</tt>.
        /// </para>
        /// <para>
        /// This is made to go together with <see cref="RawMCFunction.Run(string)"/>
        /// using string interpolation and is otherwise not useful.
        /// </para>
        /// </summary>
        /// <param name="variable">
        /// The variable to convert to its name. This *must* be a single
        /// identifier, not a constant, the result of arithmetic, etc.
        /// </param>
        // This is implemented in the "VarNameMethodRewriter" class.
        [CustomCompiled("VarName")]
        public static extern string VarName(int variable);
    }
}
