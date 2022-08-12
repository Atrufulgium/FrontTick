using MCMirror;
using MCMirror.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler {
    /// <summary>
    /// <para>
    /// This walker collects all MCFunction-tagged methods in this syntax tree
    /// and at the same time checks whether they all satisfy the
    /// <c>static void(void)</c> signature and whether their optional name
    /// is legal.
    /// </para>
    /// <para>
    /// This walker also finds all CustomCompiled-tagged methods.
    /// </para>
    /// </summary>
    internal class FindEntryPointsWalker : CSharpSyntaxWalker, ICustomDiagnosable {

        public HashSet<EntryPoint> foundMethods;
        public ReadOnlyCollection<Diagnostic> CustomDiagnostics => new(customDiagnostics);
        List<Diagnostic> customDiagnostics;

        public void AddCustomDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
            => customDiagnostics.Add(Diagnostic.Create(descriptor, location, messageArgs));

        SemanticModel semantics;
        NameManager nameManager;

        public FindEntryPointsWalker(SemanticModel semantics, NameManager nameManager) {
            this.semantics = semantics;
            this.nameManager = nameManager;
            foundMethods = new HashSet<EntryPoint>();
            customDiagnostics = new List<Diagnostic>();
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax method) {
            // Before doing the normal path, check first if it's a custom compiled method.
            // This is so hopelessly coupled with NameManager.cs
            if (semantics.TryGetSemanticAttributeOfType(method, typeof(CustomCompiledAttribute), out _)) {
                nameManager.RegisterMethodname(semantics, method, this);
                return;
            }

            if (semantics.TryGetSemanticAttributeOfType(method, typeof(MCFunctionAttribute), out _)) {
                // Check whether the signature is correct.
                bool hasStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword);
                bool voidIn = method.ArityOfArguments() == 0;
                bool voidOut = method.ReturnType.ChildTokensContain(SyntaxKind.VoidKeyword);
                if (!(hasStatic && voidIn && voidOut)) {
                    this.AddCustomDiagnostic(
                        DiagnosticRules.MCFunctionAttributeIncorrect,
                        method,
                        method.Identifier.Text
                    );
                }
            }
            if (!nameManager.RegisterMethodname(semantics, method, this))
                return;
            // We need to add *all* methods as entry points, as they reference
            // eachother. Only later can we find out which are actually used.
            foundMethods.Add(new EntryPoint(semantics, method));
        }
    }
}
