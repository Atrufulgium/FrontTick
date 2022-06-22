using Atrufulgium.FrontTick.Compiler.FullRewriters;
using Atrufulgium.FrontTick.Compiler.FullWalkers;
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
    public interface IFullVisitor {
        /// <summary>
        /// Visit all trees this compiler cares about.
        /// </summary>
        public void FullVisit();

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
        /// Whether this visitor only reads, or does both reading and writing
        /// operations.
        /// </summary>
        public bool ReadOnly { get; }
    }
}
