using System;

namespace MCMirror.Internal {
    public static class RawMCFunction {
        /// <summary>
        /// Any compile-time constant argument code is pasted as-is in the
        /// compiled file.
        /// This is useful both for low-level code that other code builds upon,
        /// and in case MCMirror is incomplete somehow.
        /// </summary>
        /// <param name="code">
        /// The code to be literally run.
        /// </param>
        /// <remarks>
        /// Whenever you, the programmer, use this, please consider creating
        /// an issue in the repo if it isn't there already! You should really
        /// only need this in case MCMirror is lacking some feature.
        /// </remarks>
        // TODO: Allow support to insert variable names, and perhaps values.
        [CustomCompiled("RunRaw")]
        public static void Run(string code) { }
    }
}
