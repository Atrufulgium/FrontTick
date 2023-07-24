namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to collect all rewriters that turn easy c# constructs into
    /// obnoxious methods.
    /// </summary>
    public class MethodifyCategory : AbstractFullWalker<
        NameConstructorsCategory,
        PropertyCategory,
        NameCastsCategory,
        NameOperatorsCategory,
        StaticifyInstanceCategory
        > { }
}
