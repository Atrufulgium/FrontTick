using Microsoft.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler {
    // As if this project will get far enough to need internationalisation.
    /// <inheritdoc cref="CompilationException"/>
    public static class DiagnosticRules {

        static DiagnosticDescriptor Create(string id, string title, string messageFormat)
            => new DiagnosticDescriptor(id, title, messageFormat, category: "", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static DiagnosticDescriptor MCFunctionAttributeIncorrect
            => Create(
                id: "FT0001",
                title: "Methods attributed [MCfunction()] must have signature static void(void).",
                messageFormat: "Make sure that [MCFunction()]-attributed '{0}' is static, returns nothing, and has no arguments."
            );

        public static DiagnosticDescriptor MCFunctionAttributeIllegalName
            => Create(
                id: "FT0002",
                title: "MCFunction names must only use [a-z0-9/._-] characters and not be empty.",
                messageFormat: "Method '{0}' has an MCFunction name which uses a non-[a-z0-9/._-] character or is empty. Please only use those characters in the MCFunction attribute, or only [a-zA-Z0-9_] in the c# method names."
            );

        public static DiagnosticDescriptor MCFunctionMethodNameClash
            => Create(
                id: "FT0003",
                title: "Two different methods ended up with the same MCFunction name.",
                messageFormat: "Two different methods\n  '{0}'; and\n  '{1}'\ncompiled to the same MCFunction name:\n  '{2}'.\nNote that uppercase gets converted to lower case, and a lot of characters gt stripped, so try are more significant difference.."
            );

        public static DiagnosticDescriptor MCFunctionMethodNameNotRegistered
            => Create(
                id: "FT0004",
                title: "Could not find a function's corresponding MCFunction name.",
                messageFormat: "Could not find the MCFunction name corresponding to '{0}'.\nThis always happens when calling compiled code (e.g. a method in System.*), but if this method is referenced in your own c# code, this is a bug on my side.\n(IL support *may* come later.)"
            );

        public static DiagnosticDescriptor ToDatapackRunRawArgMustBeLiteral
            => Create(
                id: "FT0005",
                title: "Calling `Run(string)` requires a literal, non-interpolated string.",
                messageFormat: "Calling `MCMirror.Internal.RawMCFunction.Run(string)` requires a literal, non-interpolated string. Even referencing a compile-time known constant string via identifier is not allowed."
            );
    }
}