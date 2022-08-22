namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to collect all setup visitors into a single class with
    /// dependencies.
    /// </summary>
    public class SetupCategory : AbstractFullRewriter<
        ApplyNoCompileAttributeRewriter,
        RegisterMethodsWalker
        > { }
}
