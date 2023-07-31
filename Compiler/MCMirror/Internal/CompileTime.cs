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
        [CustomCompiled("CompileTime/VarName")]
        public static extern string VarName(int variable);

        /// <summary>
        /// <para>
        /// Converts a method call into the name you <tt>/function</tt> call
        /// it with. For instance calling this with as argument a method
        /// <code>
        ///     [MCFunction("my-method")]
        ///     public static void SomeMethod() { .. }
        /// </code>
        /// would make this output <tt>"#namespace:my-method</tt>.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Stupid thing doesn't work. Get an OverloadResolutionFailure with 1
        /// candidate -- isn't that in any case sufficient when all that's
        /// asked for is a delegate?
        /// </remarks>
        // This is also implemented in the "VarNameMethodRewriter" class.
        [CustomCompiled("CompileTime/MethodName")]
        public static extern string MethodName(System.Delegate method);

        /// <summary>
        /// Returns the current compiling mcfunction namespace.
        /// </summary>
        [CustomCompiled("CompileTime/CurrentNamespace")]
        public static extern string CurrentNamespace();

        /// <summary>
        /// Returns a number that allows differs each compilation. This allows
        /// TrueLoad to realise recompilations.
        /// </summary>
        [CustomCompiled("CompileTime/TrueLoadValue")]
        public static extern string TrueLoadValue();
    }
}
