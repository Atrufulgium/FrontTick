namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Removes all nontrivial <tt>new</tt>s and replaces them with method calls.
    /// </para>
    /// <para>
    /// Also does the other constructor processing we need.
    /// </para>
    /// </summary>
    public class NameConstructorsCategory : AbstractFullRewriter<
        MemberInitToConstructors,
        CopyConstructorsToNamedRewriter,
        ConstructorsToMethodCallsRewriter,
        RemoveConstructorsRewriter
    > { }
}
