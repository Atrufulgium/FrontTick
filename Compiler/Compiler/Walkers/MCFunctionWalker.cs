using MCMirror;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Atrufulgium.FrontTick.Compiler.Walkers {
    /// <summary>
    /// This walker collects all MCFunction-tagged methods in this syntax tree
    /// and at the same time checks whether they all satisfy the
    /// <c>static void(void)</c> signature and whether their optional name
    /// is legal.
    /// </summary>
    internal class MCFunctionWalker : CSharpSyntaxWalker {

        public HashSet<EntryPoint> foundMethods;
        public List<Diagnostic> customDiagnostics;

        SemanticModel semantics;

        public MCFunctionWalker(SemanticModel semantics) {
            this.semantics = semantics;
            foundMethods = new HashSet<EntryPoint>();
            customDiagnostics = new List<Diagnostic>();
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax method) {
            if (method.TryGetSemanticAttributeOfType(typeof(MCFunctionAttribute), semantics, out var attrib)) {
                // Check whether the signature is correct.
                bool hasStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword);
                bool voidIn = method.ArityOfArguments() == 0;
                bool voidOut = method.ReturnType.ChildTokensContain(SyntaxKind.VoidKeyword);
                if (hasStatic && voidIn && voidOut) {
                    foundMethods.Add(new EntryPoint(new SyntaxSemanticsPair(semantics), method));
                } else {
                    customDiagnostics.Add(Diagnostic.Create(
                        DiagnosticRules.MCFunctionAttributeIncorrect,
                        method.GetLocation(),
                        method.Identifier.Text
                    ));
                }

                // Check whether the custom name, if it is there, is legal.
                if (attrib.ConstructorArguments.Length > 0) {
                    string name = (string)attrib.ConstructorArguments[0].Value;
                    if (name != DatapackFile.NormalizeFunctionName(name) || name == "") {
                        customDiagnostics.Add(Diagnostic.Create(
                            DiagnosticRules.MCFunctionAttributeIllegalName,
                            method.GetLocation(),
                            method.Identifier.Text, name
                        ));
                    }
                }
            }
        }
    }
}
