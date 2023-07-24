namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Removes all nontrivial <tt>new</tt>s and replaces them with method calls.
    /// </para>
    /// </summary>
    public class NameConstructorsCategory : AbstractFullRewriter<
        CopyConstructorsToNamedRewriter,
        ConstructorsToMethodCallsRewriter,
        RemoveConstructorsRewriter
    > { }
}
