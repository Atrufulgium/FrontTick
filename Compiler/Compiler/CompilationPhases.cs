using Atrufulgium.FrontTick.Compiler.FullRewriters;
using Atrufulgium.FrontTick.Compiler.FullWalkers;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// This class will contain the various possibilities for how to compile,
    /// both from a "how many optimisations do you want?"-perspective, as from
    /// maybe even a "what Minecraft version is supported?"-perspective.
    /// </summary>
    public static class CompilationPhases {
        /// <summary>
        /// If fed to <see cref="Compiler.SetCompilationPhases(IEnumerable{IFullVisitor})"/>,
        /// this compiler will do the bare-bones minimum. This is the default.
        /// </summary>
        public static IFullVisitor[] BasicCompilationPhases(Compiler c) => new IFullVisitor[] {
                new ProcessedToDatapackWalker(c)
            };

        /// <summary>
        /// If fed to <see cref="Compiler.SetCompilationPhases(IEnumerable{IFullVisitor})"/>,
        /// this will do as much as it can.
        /// </summary>
        public static IFullVisitor[] OptimisedCompilationPhases(Compiler c) => new IFullVisitor[] {
                new ProcessedToDatapackWalker(c)
            };
    }
}
