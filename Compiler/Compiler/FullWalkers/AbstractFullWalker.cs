using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Atrufulgium.FrontTick.Compiler.FullWalkers {
    /// <summary>
    /// <para>
    /// Represents a walker walking not just over all internal nodes of a
    /// syntax tree, but over all syntax trees we are considering in a full
    /// compilation process via the method <see cref="FullVisit"/>.
    /// </para>
    /// <para>
    /// These walkers can have one or multiple dependencies on previous
    /// visitors (either walkers or rewriters) in the form of generics. Whether
    /// the dependencies are satisfied is only discovered during runtime.
    /// </para>
    /// </summary>
    public abstract class AbstractFullWalker : CSharpSyntaxWalker, IFullVisitor {

        public ReadOnlyCollection<Diagnostic> CustomDiagnostics => new(customDiagnostics);
        List<Diagnostic> customDiagnostics = new();

        public bool ReadOnly => true;

        /// <summary>
        /// When using <see cref="FullVisit"/>, all data relevant to the
        /// current entry point.
        /// </summary>
        internal EntryPoint CurrentEntryPoint { get; private set; }

        internal Compiler compiler;

        public AbstractFullWalker(Compiler compiler) {
            this.compiler = compiler;
        }

        public void FullVisit() {
            foreach (var entry in compiler.entryPoints) {
                CurrentEntryPoint = entry;
                Aborted = false;
                Visit(entry.method);
            }
        }

        protected bool Aborted { get; private set; }
        /// <summary>
        /// Abort walking over the current entrypoint.
        /// </summary>
        /// <remarks>
        /// <b>You need to test for <tt>Aborted</tt> yourself</b> at the
        /// start of each Visit method and early-return. I'm not overwriting
        /// the bazillion methods you could use, nor reading roslyn-source to
        /// work out a better solution.
        /// </remarks>
        protected void Abort() {
            Aborted = true;
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullWalker<TDep1>
        : AbstractFullWalker
        where TDep1 : IFullVisitor {

        public TDep1 Dependency1 { get; private set; }

        public AbstractFullWalker(Compiler compiler) : base(compiler) {
            Dependency1 = compiler.appliedWalkers.Get<TDep1>();
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullWalker<TDep1, TDep2>
        : AbstractFullWalker<TDep1>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor {

        public TDep2 Dependency2 { get; private set; }

        public AbstractFullWalker(Compiler compiler) : base(compiler) {
            Dependency2 = compiler.appliedWalkers.Get<TDep2>();
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullWalker<TDep1, TDep2, TDep3>
        : AbstractFullWalker<TDep1, TDep2>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor {

        public TDep3 Dependency3 { get; private set; }

        public AbstractFullWalker(Compiler compiler) : base(compiler) {
            Dependency3 = compiler.appliedWalkers.Get<TDep3>();
        }
    }

    /// <inheritdoc/>
    // no you're not gonna need more dependencies.
    public abstract class AbstractFullWalker<TDep1, TDep2, TDep3, TDep4>
        : AbstractFullWalker<TDep1, TDep2, TDep3>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor {

        public TDep4 Dependency4 { get; private set; }

        public AbstractFullWalker(Compiler compiler) : base(compiler) {
            Dependency4 = compiler.appliedWalkers.Get<TDep4>();
        }
    }
}
