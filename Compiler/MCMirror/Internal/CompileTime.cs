namespace MCMirror.Internal {
    /// <summary>
    /// A class of methods whose effect is only at compile-time.
    /// </summary>
    // This is implemented in the "CompileTimeClassRewriter" class.
    public static class CompileTime {
        /// <summary>
        /// <para>
        /// Converts an variable into the name the local context uses in
        /// MCFunction to access its value. For instance, a method parameter
        /// turns into <tt>"#namespace:class.method##arg0"</tt>.
        /// </para>
        /// <para>
        /// This is made to go together with <see cref="RawMCFunction.Run(string)"/>
        /// using string interpolation and is otherwise not useful. (You may be
        /// tempted to extract it if you use it multiple times, but as strings
        /// are not supported...)
        /// </para>
        /// </summary>
        /// <param name="variable">
        /// The variable to convert to its name. This *must* be a single
        /// identifier, not a constant, the result of arithmetic, etc.
        /// </param>
        [CustomCompiled("CompileTime/VarNameInt")]
        [CompilerUsesName]
        public static extern string VarName(int variable);

        /// <inheritdoc cref="VarName(int)"/>
        [CustomCompiled("CompileTime/VarNameBool")]
        public static extern string VarName(bool variable);

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
        // TODO: Fix, apparantly.
        [CustomCompiled("CompileTime/MethodName")]
        public static extern string MethodName(System.Delegate method);

        /// <summary>
        /// Gives a "<c>DD/MM/YYYY hh:mm:ss</c>" string generated during
        /// compilation.
        /// </summary>
        [CustomCompiled("CompileTime/Timestamp")]
        public static extern string ApproximateCompilationTimestamp();

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

        /// <summary>
        /// <para>
        /// Puts the current value of <paramref name="value"/> into chat for <c>@a</c>.
        /// </para>
        /// <para>
        /// Meant for internal debug purposes only, please use [doesn't exist
        /// yet] for proper logging into chat.
        /// </para>
        /// </summary>
        [CustomCompiled("CompileTime/Print")]
        [CompilerUsesName]
        public static extern string Print(int value);

        /// <inheritdoc cref="Print(int)"/>
        [CustomCompiled("CompileTime/PrintComplex")]
        [CompilerUsesName]
        public static extern string Print(object value);
    }
}
