using Atrufulgium.FrontTick.Compiler.Visitors;
using Microsoft.CodeAnalysis;
using System.Collections.ObjectModel;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// A pretty hacky interface to unify the <see cref="AbstractFullWalker"/>
    /// and <see cref="AbstractFullRewriter"/> classes for what I need them:
    /// being arbitrary reads and readwrites on the multiple trees I compile.
    /// </summary>
    /// <remarks>
    /// Don't implement this manually anywhere else. I'm assuming the
    /// implementors being *just* those two classes and their inheritors.
    /// </remarks>
    public interface IFullVisitor : ICustomDiagnosable {
        /// <summary>
        /// Before doing anything with this instance, the compiler should be
        /// set with this method for any setup work.
        /// </summary>
        public void SetCompiler(Compiler c);

        /// <summary>
        /// Visit all trees this compiler cares about.
        /// </summary>
        public void FullVisit();

        /// <summary>
        /// Whether this visitor only reads, or does both reading and writing
        /// operations.
        /// </summary>
        public bool ReadOnly { get; }
    }

    public interface ICustomDiagnosable {
        /// <summary>
        /// <para>
        /// All custom diagnostics encountered during compilation, in
        /// chronological order.
        /// </para>
        /// <para>
        /// This is not an exhaustive list, as sometimes diagnostics prevent
        /// further processing that would've also raised further diagnostics.
        /// </para>
        /// </summary>
        public ReadOnlyCollection<Diagnostic> CustomDiagnostics { get; }
        /// <summary>
        /// This should just shortcut adding a diagnostic via Diagnostic.Create
        /// </summary>
        public void AddCustomDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs);
    }

    public static class CustomDiagnosableExtensions {
        public static void AddCustomDiagnostic(
            this ICustomDiagnosable diagnosticsOutput,
            DiagnosticDescriptor descriptor,
            SyntaxNode node,
            params object[] messageArgs)
            => diagnosticsOutput.AddCustomDiagnostic(descriptor, node.GetLocation(), messageArgs);
    }
}
