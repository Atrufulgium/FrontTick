using MCMirror.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Represents a rewriter writing not just over all internal nodes of a
    /// syntax tree, but over all syntax trees we are considering in a full
    /// compilation process via the method <see cref="FullVisit"/>.
    /// </para>
    /// <para>
    /// These rewriters can have one or multiple dependencies on previous
    /// visitors (either walkers or rewriters) in the form of generics.
    /// These are added automatically when missing. It is recommended to use
    /// their values not via <tt>DependencyX</tt> but with an intermediate
    /// <tt>TDepX SomeProperName => DependencyX</tt> for clarity.
    /// </para>
    /// </summary>
    /// <remarks>
    /// In order not to get any confusion with any other source generators, not
    /// get clashes with existing names, and allow literal name translation
    /// support to MCFunction, all non-method identifiers introduced by these
    /// methods should be prefixed with a '#'.
    /// (cf. vanilla c# prefixing &lt;&gt; with e.g. generated yield classes.)
    /// </remarks>
    public abstract class AbstractFullRewriter : CSharpSyntaxRewriter, IFullVisitor {

        public ReadOnlyCollection<Diagnostic> CustomDiagnostics => new(customDiagnostics);
        List<Diagnostic> customDiagnostics = new();
        protected NameManager nameManager;

        public bool ReadOnly => false;
        public void AddCustomDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
            => customDiagnostics.Add(Diagnostic.Create(descriptor, location, messageArgs));

        /// <summary>
        /// When using <see cref = "FullVisit" />, the current
        /// entry point's semantic model.
        /// </summary>
        internal SemanticModel CurrentSemantics { get; private set; }

        internal Compiler compiler;

        public virtual void SetCompiler(Compiler compiler) {
            this.compiler = compiler;
            nameManager = compiler.nameManager;
        }

        // Do not handle any [NoCompile] code.
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node) {
            if (CurrentSemantics.TryGetAttributeOfType(node, typeof(NoCompileAttribute), out _))
                return null;
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node) {
            if (CurrentSemantics.TryGetAttributeOfType(node, typeof(NoCompileAttribute), out _))
                return null;
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node) {
            if (CurrentSemantics.TryGetAttributeOfType(node, typeof(NoCompileAttribute), out _))
                return null;
            return base.VisitStructDeclaration(node);
        }

        public void FullVisit() {
            // We will be modifying the compiler roots because of updates,
            // so iterate a copy instead.
            GlobalPreProcess();
            foreach (var entry in compiler.roots.ToArray()) {
                CurrentSemantics = entry;
                PreProcess();
                var result = Visit(entry.SyntaxTree.GetCompilationUnitRoot());
                compiler.ReplaceTree(entry, result);
                PostProcess();
            }
            GlobalPostProcess();
        }

        /// <summary>
        /// For each syntax tree, this is called just before it's visited.
        /// </summary>
        /// <remarks> No need to call the base method. </remarks>
        public virtual void PreProcess() { }
        /// <summary>
        /// For each syntax tree, this is called just after it's been visited
        /// and the changes have been written to the compiler.
        /// </summary>
        /// <remarks> No need to call the base method. </remarks>
        public virtual void PostProcess() { }
        /// <summary>
        /// This is called just before any syntax tree is visited.
        /// </summary>
        /// <remarks> No need to call the base method. </remarks>
        public virtual void GlobalPreProcess() { }
        /// <summary>
        /// This is called just after all syntax trees have been visited and
        /// all changes have been written to the compiler.
        /// </summary>
        /// <remarks> No need to call the base method. </remarks>
        public virtual void GlobalPostProcess() { }
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

    /// <inheritdoc/>
    public abstract class AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4, TDep5>
        : AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor
        where TDep5 : IFullVisitor {

        public TDep5 Dependency5 { get; private set; }

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
            Dependency5 = compiler.appliedWalkers.Get<TDep5>();
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6>
        : AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4, TDep5>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor
        where TDep5 : IFullVisitor
        where TDep6 : IFullVisitor {

        public TDep6 Dependency6 { get; private set; }

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
            Dependency6 = compiler.appliedWalkers.Get<TDep6>();
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6, TDep7>
        : AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor
        where TDep5 : IFullVisitor
        where TDep6 : IFullVisitor
        where TDep7 : IFullVisitor {

        public TDep7 Dependency7 { get; private set; }

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
            Dependency7 = compiler.appliedWalkers.Get<TDep7>();
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6, TDep7, TDep8>
        : AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6, TDep7>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor
        where TDep5 : IFullVisitor
        where TDep6 : IFullVisitor
        where TDep7 : IFullVisitor
        where TDep8 : IFullVisitor {

        public TDep8 Dependency8 { get; private set; }

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
            Dependency8 = compiler.appliedWalkers.Get<TDep8>();
        }
    }

    /// <inheritdoc/>
    public abstract class AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6, TDep7, TDep8, TDep9>
        : AbstractFullRewriter<TDep1, TDep2, TDep3, TDep4, TDep5, TDep6, TDep7, TDep8>
        where TDep1 : IFullVisitor
        where TDep2 : IFullVisitor
        where TDep3 : IFullVisitor
        where TDep4 : IFullVisitor
        where TDep5 : IFullVisitor
        where TDep6 : IFullVisitor
        where TDep7 : IFullVisitor
        where TDep8 : IFullVisitor
        where TDep9 : IFullVisitor {

        public TDep9 Dependency9 { get; private set; }

        public override void SetCompiler(Compiler compiler) {
            base.SetCompiler(compiler);
            Dependency9 = compiler.appliedWalkers.Get<TDep9>();
        }
    }
}
