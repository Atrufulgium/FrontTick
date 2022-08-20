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
            
            string name;

            // Before doing the normal path, check first if it's a custom compiled method.
            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(CustomCompiledAttribute), out var attrib)) {
                // No error checking this branch whatsoever because really, I'm me.
                name = (string)attrib.ConstructorArguments[0].Value;
                nameManager.RegisterMethodname(CurrentSemantics, method, name, this, prefixNamespace: false);
                return;
            }

            // Try whether the sig is correct, and then store the method into here.
            string fullyQualifiedName = NameManager.GetFullyQualifiedMethodName(CurrentSemantics, method);

            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(MCFunctionAttribute), out attrib)) {
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
                if (attrib.ConstructorArguments.Length == 0) {
                    name = fullyQualifiedName;
                    name = NameManager.NormalizeFunctionName(name);
                } else {
                    // Check whether the custom name is legal as mcfunction
                    name = (string)attrib.ConstructorArguments[0].Value;
                    if (name != NameManager.NormalizeFunctionName(name) || name == "") {
                        AddCustomDiagnostic(
                            DiagnosticRules.MCFunctionAttributeIllegalName,
                            method.GetLocation(),
                            method.Identifier.Text
                        );
                        return;
                    }
                }
            } else {
                name = "internal/" + fullyQualifiedName;
                name = NameManager.NormalizeFunctionName(name);
            }
            nameManager.RegisterMethodname(CurrentSemantics, method, name, this);
        }
    }
}
