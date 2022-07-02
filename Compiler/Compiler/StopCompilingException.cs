using Microsoft.CodeAnalysis;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// This exception is very "special" and does two things:
    /// (1) Raise a normal exception that is only to be caught by <see cref="Compiler"/>.
    /// (2) Log a diagnostic that caused this problem that requires us to
    ///     immediately halt compilation (and not even finish this phase).
    /// Do prefer finishing the current phase though as that logs more user
    /// errors to display.
    /// </summary>
    /// <remarks>
    /// Do not use this some other place than <see cref="IFullVisitor"/>s.
    /// </remarks>
    internal class StopCompilingImmediatelyException : CompilationException {

        private StopCompilingImmediatelyException(string message) : base(message) { }

        public static StopCompilingImmediatelyException Create(
            ICustomDiagnosable diagnosticsOutput,
            DiagnosticDescriptor descriptor,
            Location location,
            params object[] messageArgs
        ) {
            diagnosticsOutput.AddCustomDiagnostic(descriptor, location, messageArgs);
            return new StopCompilingImmediatelyException(Diagnostic.Create(descriptor, location, messageArgs).GetMessage());
        }
        public static StopCompilingImmediatelyException Create(
            ICustomDiagnosable diagnosticsOutput,
            DiagnosticDescriptor descriptor,
            SyntaxNode node,
            params object[] messageArgs
        ) => Create(diagnosticsOutput, descriptor, node.GetLocation(), messageArgs);
    }
}
