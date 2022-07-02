using System;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// <para>
    /// The difference between <see cref="CompilationException"/>'s exceptions
    /// and <see cref="DiagnosticRules"/>' rules is that the former is not
    /// supposed to be visible to the end-user, but the latter is.
    /// </para>
    /// <para>
    /// Use the former if the code is supposed to be formatted by previous
    /// stages but may have failed somehow due to errors in the compiler, and
    /// the latter for errors in the end-user's code and not the compiler.
    /// </para>
    /// </summary>
    public class CompilationException : Exception {
        public CompilationException(string message) : base(message) { }

        public static CompilationException ToDatapackAssignmentOpsMustBeSimpleOrArithmetic
            => new("[To Datapack] Assignments must be one of \"=\", \"+=\", \"-=\", \"*=\", \"/=\", or \"%=\".");
        public static CompilationException ToDatapackAssignmentRHSsMustBeIdentifiersOrLiteralsOrCalls
            => new("[To Datapack] \"a ∘= RHS\"'s RHS must be a literal, identifier, or method call.");
        public static CompilationException ToDatapackDeclarationsMayNotBeInitializers
            => new("[To Datapack] Declarations may not be initializers.");
        public static CompilationException ToDatapackDeclarationsMustBeInMethodRootScope
            => new("[To Datapack] Declarations must be in the method's root scope.");
        public static CompilationException ToDatapackMethodCallArgumentMustBeIdentifiersOrLiterals
            => new("[To Datapack] Calls' arguments must consist of identifiers or literals.");
        public static CompilationException ToDatapackMethodCallsMustBeStatic
            => new("[To Datapack] Calls may only target static methods.");
    }
}
