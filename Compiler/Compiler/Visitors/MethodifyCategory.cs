namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to collect all rewriters that turn easy c# constructs into
    /// obnoxious methods.
    /// </summary>
    public class MethodifyCategory : AbstractFullWalker<
        PropertyCategory,
        // Constructors need to take into account `Property {get; set;} = value`.
        // It's better if those are processed already.
        NameConstructorsCategory,
        NameCastsCategory,
        NameOperatorsCategory,
        StaticifyInstanceCategory
        > { }
}
