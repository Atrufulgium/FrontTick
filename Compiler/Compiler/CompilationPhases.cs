using Atrufulgium.FrontTick.Compiler.Visitors;

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
        public static IFullVisitor[] BasicCompilationPhases => new IFullVisitor[] {
                new ArithmeticFlattenRewriter(),
                new ProcessedToDatapackWalker()
            };

        /// <summary>
        /// If fed to <see cref="Compiler.SetCompilationPhases(IEnumerable{IFullVisitor})"/>,
        /// this will do as much as it can.
        /// </summary>
        public static IFullVisitor[] OptimisedCompilationPhases => new IFullVisitor[] {
                new ArithmeticFlattenRewriter(),
                new ProcessedToDatapackWalker()
            };
    }
}
