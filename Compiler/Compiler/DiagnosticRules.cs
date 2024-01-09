using Microsoft.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler {
    // As if this project will get far enough to need internationalisation.
    /// <inheritdoc cref="CompilationException"/>
    public static class DiagnosticRules {

        static DiagnosticDescriptor Error(string id, string title, string messageFormat)
            => new(id, title, messageFormat, category: "", DiagnosticSeverity.Error, isEnabledByDefault: true);

        static DiagnosticDescriptor Warn(string id, string title, string messageFormat)
            => new(id, title, messageFormat, category: "", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static DiagnosticDescriptor Unsupported
            => Error(
                id: "FT0000",
                title: "Unsupported or unimplemented code.",
                messageFormat: "Feature '{0}' is (currently) unsupported.\nReason: {1}"
            );

        public static DiagnosticDescriptor MCFunctionAttributeIncorrect
            => Error(
                id: "FT0001",
                title: "Methods attributed [MCfunction()] must have signature static void(void).",
                messageFormat: "Make sure that [MCFunction()]-attributed '{0}' is static, returns nothing, and has no arguments."
            );

        public static DiagnosticDescriptor MCFunctionAttributeIllegalName
            => Error(
                id: "FT0002",
                title: "MCFunction names must only use [a-z0-9/._-] characters and not be empty.",
                messageFormat: "Method '{0}' has an MCFunction name which uses a non-[a-z0-9/._-] character or is empty. Please only use those characters in the MCFunction attribute, or only [a-zA-Z0-9_] in the c# method names. (In particular, uppercase letters are not allowed in the attribute argument.)"
            );

        // If you get here during testing but not during regular compilation,
        // you may have been bitten by the
        /// <see cref="Visitors.MakeCompilerTestingEasierRewriter"/>
        public static DiagnosticDescriptor MCFunctionMethodNameClash
            => Error(
                id: "FT0003",
                title: "Two different methods ended up with the same MCFunction name.",
                messageFormat: "Two different methods\n  '{0}'; and\n  '{1}'\ncompiled to the same MCFunction name:\n  '{2}'.\nNote that uppercase gets converted to lower case, and a lot of characters get stripped, so try a more significant difference."
            );

        public static DiagnosticDescriptor MCFunctionMethodNameNotRegistered
            => Error(
                id: "FT0004",
                title: "Could not find a function's corresponding MCFunction name.",
                messageFormat: "Could not find the MCFunction name corresponding to '{0}'.\nThis always happens when calling compiled code (e.g. a method in System.*), but if this method is referenced in your own c# code, this is a bug on my side.\n(IL support *may* come later.)"
            );

        public static DiagnosticDescriptor ToDatapackRunRawArgMustBeLiteral
            => Error(
                id: "FT0005",
                title: "Calling 'Run(string)' requires a literal, non-interpolated string.",
                messageFormat: "Calling 'MCMirror.Internal.RawMCFunction.Run(string)' requires a literal, non-interpolated string. Even referencing a compile-time known constant string via identifier is not allowed."
            );

        public static DiagnosticDescriptor MCTestAttributeIncorrect
            => Error(
                id: "FT0006",
                title: "Methods attributed [MCTest(AnyType)] must have signature static AnyType(void).",
                messageFormat: "Make sure that [MCTest(AnyType)]-attributed '{0}' is static and has no arguments."
            );

        public static DiagnosticDescriptor NoUnsafe
            => Error(
                id: "FT0007",
                title: "Unsafe code is unsupported.",
                messageFormat: "Everything associated with unsafe code, the keyword, pointers, spans, etc, is not supported."
            );

        public static DiagnosticDescriptor VarNameArgMustBeIdentifier
            => Error(
                id: "FT0008",
                title: "VarName(int) arguments must be identifiers.",
                messageFormat: "Calling 'MCMirror.Internal.CompileTime.VarName(int)' requires an argument that is just an identifier. No arithmetic, method calls etc."
            );

        // of the *method* Internal.CompileTime.MethodName
        public static DiagnosticDescriptor MethodNameArgMustBeIdentifier
            => Error(
                id: "FT0009",
                title: "MethodName(Delegate) arguments must be identifiers.",
                messageFormat: "Calling 'MCMirror.Internal.CompileTime.MethodName(Delegate)' requires an argument that is just an identifier."
            );

        public static DiagnosticDescriptor StringInterpolationsMustBeConstant
            => Error(
                id: "FT0010",
                title: "String interpolation values must be constant.",
                messageFormat: "When writing any a string interpolation ($\"{interpolation}\"), 'interpolation' must be a constant value."
            );

        public static DiagnosticDescriptor TickRateMustBePositive
            => Error(
                id: "FT0011",
                title: "[Tick(n)] value must be strictly positive.",
                messageFormat: "The argument in the [Tick(n)] attribute represents how many 0.05s ticks to wait each call. This must be at least one increment, but {0} < 1."
            );

        public static DiagnosticDescriptor FunctionTagAttributeMustBeStaticVoidVoid
            => Error(
                id: "FT0012",
                title: "Methods attributed [Tick()], [Load], or [TrueLoad] must have signature static void(void).",
                messageFormat: "Make sure that [Tick()], [Load], or [TrueLoad]-attributed '{0}' is static, returns nothing, and has no arguments."
            );

        // Also note that short-circuiting may be more expensive.
        public static DiagnosticDescriptor ShortCircuitingUnsupported
            => Warn(
                id: "FT0013",
                title: "Short-circuiting operators '&&' and '||' are unsupported and replaced by '&' and '|'.",
                messageFormat: "Short-circuiting ops '&&', '||', are not supported and converted to '&', '|'. If you want to short-circuit, do a manual if-check. If not, update your code to use '&' or '|' instead to not get this warning. (This behaviour may be subject to change.)"
            );

        public static DiagnosticDescriptor PrintArgMustBeIdentifier
            => Error(
                id: "FT0014",
                title: "Print(int) and Print(object) arguments must be identifiers.",
                messageFormat: "Calling 'MCMirror.Internal.CompileTime.Print()' requires an argument that is just an identifier. No arithmetic, method calls etc."
            );
    }
}