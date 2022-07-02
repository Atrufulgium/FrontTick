using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// TODO: Far future when rewriting the tree: https://stackoverflow.com/a/12168782

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    // Big note to self: On any Visit[X]() methods, if you don't call
    // base.Visit[X](), you just won't walk child methods; if you call it
    // before doing anything, you go deep -> shallow; if you call it after
    // doing everything, you go shallow -> deep.
    // The latter only makes sense if you don't rewrite.
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
    /// <remarks>
    /// In order not to get any confusion with any other source generators, not
    /// get clashes with existing names, and allow literal name translation
    /// support to MCFunction, all non-method identifiers introduced by these
    /// methods should be prefixed with a '#'.
    /// (c.f. vanilla c# prefixing &lt;&gt; with e.g. generated yield classes.)
    /// </remarks>
    public abstract class AbstractFullRewriter : CSharpSyntaxRewriter, IFullVisitor {

        public ReadOnlyCollection<Diagnostic> CustomDiagnostics => new(customDiagnostics);
        List<Diagnostic> customDiagnostics = new();
        protected NameManager nameManager;

        public bool ReadOnly => false;
        public void AddCustomDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
            => customDiagnostics.Add(Diagnostic.Create(descriptor, location, messageArgs));

        /// <inheritdoc cref="AbstractFullWalker.CurrentEntryPoint"/>
        internal EntryPoint CurrentEntryPoint { get; private set; }
        /// <inheritdoc cref="AbstractFullWalker.CurrentSemantics"/>
        internal SemanticModel CurrentSemantics { get; private set; }

        internal Compiler compiler;

        public virtual void SetCompiler(Compiler compiler) {
            this.compiler = compiler;
            nameManager = compiler.nameManager;
        }

        public void FullVisit() {
            foreach (var entry in compiler.entryPoints) {
                CurrentEntryPoint = entry;
                CurrentSemantics = entry.semantics;
                Aborted = false;
                Visit(entry.method);
            }
        }

        protected bool Aborted { get; private set; }
        /// <inheritdoc cref="AbstractFullWalker.Abort"/>
        protected void Abort() {
            Aborted = true;
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullRewriter<TDep1>
        : AbstractFullRewriter
        where TDep1 : IFullVisitor {

        public TDep1 Dependency1 { get; private set; }

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
            Dependency1 = compiler.appliedWalkers.Get<TDep1>();
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullRewriter<TDep1, TDep2>
        : AbstractFullRewriter<TDep1>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor {

        public TDep2 Dependency2 { get; private set; }

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
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

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
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

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
            Dependency4 = compiler.appliedWalkers.Get<TDep4>();
        }
    }
}
