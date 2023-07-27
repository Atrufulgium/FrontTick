namespace MCMirror.Internal {
    public static class RawMCFunction {
        /// <summary>
        /// Any compile-time constant argument code is pasted as-is in the
        /// compiled file.
        /// This is useful both for low-level code that other code builds upon,
        /// and in case MCMirror is incomplete somehow.
        /// </summary>
        /// <param name="code">
        /// The mcfunction code to be literally run. This is not checked for
        /// correctness, and pasted as-is into the datapack.
        /// </param>
        /// <remarks>
        /// Whenever you, the programmer, use this, please consider creating
        /// an issue in the repo if it isn't there already! You should really
        /// only need this in case MCMirror is lacking some feature.
        /// </remarks>
        // This is implemented in the "ProcessedToDatapackWalker" class.
#pragma warning disable IDE0060 // Remove unused parameter
        [CustomCompiled("RunRaw")]
        public static extern void Run(string code);
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
