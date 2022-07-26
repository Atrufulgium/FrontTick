namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// As <see cref="ProcessedToDatapackWalker"/> needs the information of all
    /// gotos, this class preprocesses that by telling every block what gotos
    /// are contained somewhere within.
    /// </summary>
    public class GotoLabelerWalker : AbstractFullWalker {

    }
}
