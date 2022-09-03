namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to transform for/while/do while loops into goto loops.
    /// </summary>
    public class LoopsToGotoCategory : AbstractFullRewriter<
        GuaranteeBlockRewriter,
        ForToWhileRewriter,
        DoWhileToWhileRewriter,
        WhileToGotoRewriter> {
        // TODO: In order to not break on the `break;` in switches, require those to be processed earlier!
    }
}
