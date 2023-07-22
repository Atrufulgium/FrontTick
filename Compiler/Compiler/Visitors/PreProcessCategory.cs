namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to collect all visitors that turn general c# into the specific
    /// c# <see cref="ProcessedToDatapackWalker"> needs into a single class
    /// with dependencies.
    /// </summary>
    public class PreProcessCategory : AbstractFullWalker<
        MethodifyCategory,
        VarNameMethodRewriter,
        GuaranteeBlockRewriter,
        LoopsToGotoCategory,
        IfTrueFalseRewriter,
        SimplifyIfConditionRewriter
        > { }
}
