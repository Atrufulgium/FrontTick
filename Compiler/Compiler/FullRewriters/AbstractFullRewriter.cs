using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// TODO: Far future when rewriting the tree: https://stackoverflow.com/a/12168782

namespace Atrufulgium.FrontTick.Compiler.FullRewriters {
    /// <summary>
    /// <para>
    /// Represents a rewriter writing not just over all internal nodes of a
    /// syntax tree, but over all syntax trees we are considering in a full
    /// compilation process via the method <see cref="FullVisit"/>.
    /// </para>
    /// <para>
    /// These rewriters can have one or multiple dependencies on previous
    /// visitors (either walkers or rewriters) in the form of generics. Whether
    /// the dependencies are satisfied is only discovered during runtime.
    /// </para>
    /// </summary>
    public abstract class AbstractFullRewriter : CSharpSyntaxRewriter, IFullVisitor {

        public ReadOnlyCollection<Diagnostic> CustomDiagnostics => new(customDiagnostics);
        List<Diagnostic> customDiagnostics = new();

        public bool ReadOnly => false;

        /// <inheritdoc cref="FullWalkers.AbstractFullWalker.CurrentEntryPoint"/>
        internal EntryPoint CurrentEntryPoint { get; private set; }

        internal Compiler compiler;

        public AbstractFullRewriter(Compiler compiler) {
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
        /// <inheritdoc cref="FullWalkers.AbstractFullWalker.Abort"/>
        protected void Abort() {
            Aborted = true;
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullRewriter<TDep1>
        : AbstractFullRewriter
        where TDep1 : IFullVisitor {

        public TDep1 Dependency1 { get; private set; }

        public AbstractFullRewriter(Compiler compiler) : base(compiler) {
            Dependency1 = compiler.appliedWalkers.Get<TDep1>();
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullRewriter<TDep1, TDep2>
        : AbstractFullRewriter<TDep1>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor {

        public TDep2 Dependency2 { get; private set; }

        public AbstractFullRewriter(Compiler compiler) : base(compiler) {
            Dependency2 = compiler.appliedWalkers.Get<TDep2>();
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullRewriter<TDep1, TDep2, TDep3>
        : AbstractFullRewriter<TDep1, TDep2>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor {

        public TDep3 Dependency3 { get; private set; }

        public AbstractFullRewriter(Compiler compiler) : base(compiler) {
            Dependency3 = compiler.appliedWalkers.Get<TDep3>();
        }
    }

    /// <inheritdoc/>
    // no you're not gonna need more dependencies.
    public abstract class AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4>
        : AbstractFullRewriter<TDep1, TDep2, TDep3>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor {

        public TDep4 Dependency4 { get; private set; }

        public AbstractFullRewriter(Compiler compiler) : base(compiler) {
            Dependency4 = compiler.appliedWalkers.Get<TDep4>();
        }
    }
}
