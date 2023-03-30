namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This category turns both explicit and implicit casts into methods that
    /// are called explicitely.
    /// </para>
    /// </summary>
    public class NameCastsCategory : AbstractFullRewriter<
        CopyCastsToNamedRewriter,
        CastsToMethodCallsRewriter,
        RemoveCastRewriter
    > { }
}
