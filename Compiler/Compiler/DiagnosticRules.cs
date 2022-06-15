using Microsoft.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler {
    // As if this project will get far enough to need internationalisation.
    public static class DiagnosticRules {
        /// <summary>
        /// "Methods attributed [MCfunction()] must have signature static void(void)."
        /// </summary>
        public static DiagnosticDescriptor MCFunctionAttributeIncorrect
            => new DiagnosticDescriptor(
                id: "FT0001",
                title: "Methods attributed [MCfunction()] must have signature static void(void).",
                messageFormat: "Make sure that [MCFunction()]-attributed '{0}' is static, returns nothing, and has no arguments.",
                category: "",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

        /// <summary>
        /// "MCFunction names must only use [a-z0-9/._-] characters and not be empty."
        /// </summary>
        public static DiagnosticDescriptor MCFunctionAttributeIllegalName
            => new DiagnosticDescriptor(
                id: "FT0002",
                title: "MCFunction names must only use [a-z0-9/._-] characters and not be empty.",
                messageFormat: "Method '{0}' has MCFunction name '{1}', which uses a non-[a-z0-9/._-] character or is empty.",
                category: "",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );


    }
}