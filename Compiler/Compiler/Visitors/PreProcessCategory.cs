namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <inheritdoc cref="PreProcessCategory"/>
    public class PreProcessCategory2 : AbstractCategory<
        RemoveParenthesesRewriter,
        RewritePrimitiveLiteralsRewriter
        > { }

    /// <summary>
    /// This is to collect all visitors that turn general c# into the specific
    /// c# <see cref="ProcessedToDatapackWalker"> needs into a single class
    /// with dependencies.
    /// </summary>
    public class PreProcessCategory : AbstractCategory<
        PreProcessCategory2,
        SplitDeclarationInitializersRewriter,
        ShortCircuitOperatorRewriter,
        MethodifyCategory,
        CompiletimeInterpolationRewriter,
        GuaranteeBlockRewriter,
        LoopsToGotoCategory,
        IfTrueFalseRewriter,
        SimplifyIfConditionRewriter
        > { }
}
