namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to collect all property-related rewriters into one class.
    /// (Also affects all other arrow statements, but that's fine.)
    /// </summary>
    /// <remarks>
    /// This introduces arithmetic of the form `a ∘ b`, so put this before
    /// anything handling that. Yes I need an <tt>IBefore&lt;..&gt;</tt>.
    /// </remarks>
    public class PropertyCategory : AbstractFullWalker<
        ArrowRewriter,
        AutoPropertyRewriter,
        CopyPropertiesToNamedRewriter,
        PropertiesToMethodCallsRewriter,
        RemovePropertiesRewriter
        > { }
}
