namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to collect all setup visitors into a single class with
    /// dependencies. These may also do things like introduce classes with
    /// whatever code is needed.
    /// </summary>
    public class SetupCategory : AbstractCategory<
        RegisterMethodsWalker,
        HandleTestBodiesCategory
        > { }
}
