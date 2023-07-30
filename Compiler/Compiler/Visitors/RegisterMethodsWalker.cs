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

        public override void VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax method) {
            base.VisitMethodDeclarationRespectingNoCompile(method);
            
            string name = null;
            bool isInternal = false;

            // Before doing the normal path, check first if it's a custom compiled method.
            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(CustomCompiledAttribute), out var attrib)) {
                // No error checking this branch whatsoever because really, I'm me.
                // Note that this doesn't get the "isInternal" true prefix because the results aren't stored
                // to a datapack, because this is custom compilation.
                name = (string)attrib.ConstructorArguments[0].Value;
                nameManager.RegisterMethodname(CurrentSemantics, method, this, name: name, prefixNamespace: false, suffixParams: false);
                return;
            }

            // Try whether the sig is correct, and then store the method into here.

            if (CurrentSemantics.TryGetSemanticAttributeOfType(method, typeof(MCFunctionAttribute), out attrib)) {
                // Check whether the signature is correct.
                bool hasStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword);
                bool voidIn = method.ArityOfArguments() == 0;
                bool voidOut = method.ReturnsVoid();
                if (!(hasStatic && voidIn && voidOut)) {
                    this.AddCustomDiagnostic(
                        DiagnosticRules.MCFunctionAttributeIncorrect,
                        method,
                        method.Identifier.Text
                    );
                }
                // Null defaults to the fully qualified name.
                // Otherwise, custom name.
                if (attrib.ConstructorArguments.Length != 0) {
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
                isInternal = true;
            }
            nameManager.RegisterMethodname(CurrentSemantics, method, this, name: name, isInternal: isInternal);
        }
    }
}
