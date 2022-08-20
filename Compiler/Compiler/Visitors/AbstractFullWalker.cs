using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    // Big note to self: On any Visit[X]() methods, if you don't call
    // base.Visit[X](), you just won't walk child methods; if you call it
    // before doing anything, you go deep -> shallow; if you call it after
    // doing everything, you go shallow -> deep.
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
        protected NameManager nameManager;

        public bool ReadOnly => true;
        public void AddCustomDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
            => customDiagnostics.Add(Diagnostic.Create(descriptor, location, messageArgs));

        /// <summary>
        /// When using <see cref="FullVisit"/>, the current entry point's
        /// semantic model.
        /// </summary>
        internal SemanticModel CurrentSemantics { get; private set; }

        internal Compiler compiler;

        public virtual void SetCompiler(Compiler compiler) {
            this.compiler = compiler;
            nameManager = compiler.nameManager;
        }

        public void FullVisit() {
            foreach (var entry in compiler.roots) {
                CurrentSemantics = entry;
                Visit(entry.SyntaxTree.GetCompilationUnitRoot());
            }
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullWalker<TDep1>
        : AbstractFullWalker
        where TDep1 : IFullVisitor {

        public TDep1 Dependency1 { get; private set; }

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
            Dependency1 = compiler.appliedWalkers.Get<TDep1>();
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullWalker<TDep1, TDep2>
        : AbstractFullWalker<TDep1>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor {

        public TDep2 Dependency2 { get; private set; }

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
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

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
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

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
            Dependency4 = compiler.appliedWalkers.Get<TDep4>();
        }
    }
}
