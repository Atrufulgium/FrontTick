using System;

namespace MCMirror.Internal {
    /// <summary>
    /// This signifies that a type name is used in the compiler (and
    /// specifically in the MCMirrorTypeNames class).
    /// This correspondence is not handled automatically (yet), so for now
    /// consider this attribute a "don't freely modify!"-warning.
    /// </summary>
    [CompilerUsesName]
    [NoCompile]
    internal class CompilerUsesName : Attribute { }
}
