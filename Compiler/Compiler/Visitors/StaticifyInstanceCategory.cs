namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// This category turns instance methods into static methods.
    /// </para>
    /// </summary>
    public class StaticifyInstanceCategory : AbstractFullRewriter<
        ThisRewriter,
        CopyInstanceToStaticCallsRewriter,
        InstanceToStaticCallRewriter,
        RemoveInstanceCallsRewriter
    > { }
}
