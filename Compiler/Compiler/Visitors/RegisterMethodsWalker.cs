using MCMirror;
using MCMirror.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// Registers all methods for use with <see cref="NameManager"/>.
    /// </summary>
    // Note that this does not rewrite the tree to remove any [CustomCompiled]
    // attributed methods. This because of program correctness -- only in the
    // ToDatapack phase do we ignore any illegal-named methods. And those
    // custom compiled methods certainly are illegally named.
    public class RegisterMethodsWalker : AbstractFullWalker {

        public override void VisitMethodDeclaration(MethodDeclarationSyntax method) {
            base.VisitMethodDeclaration(method);

            // Before doing the normal path, check first if it's a custom compiled method.
            // This is so hopelessly coupled with NameManager.cs
            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(CustomCompiledAttribute), out _)) {
                nameManager.RegisterMethodname(CurrentSemantics, method, this);
                return;
            }

            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(MCFunctionAttribute), out _)) {
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
            nameManager.RegisterMethodname(CurrentSemantics, method, this);
        }
    }
}
