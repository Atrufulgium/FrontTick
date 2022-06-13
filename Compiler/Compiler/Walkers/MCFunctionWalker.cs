using MCMirror;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Atrufulgium.FrontTick.Compiler.Walkers {
    internal class MCFunctionWalker : CSharpSyntaxWalker {

        public HashSet<MethodDeclarationSyntax> mcFunctionMethods;

        SemanticModel semantics;
        INamedTypeSymbol mcFunctionType;

        public MCFunctionWalker(SemanticModel semantics) {
            this.semantics = semantics;
            mcFunctionType = semantics.Compilation.GetTypeByMetadataName(typeof(MCFunctionAttribute).FullName);
            mcFunctionMethods = new HashSet<MethodDeclarationSyntax>();
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax method) {
            // An AttributeList is a full
            // `[LoremIpsum("Dolor Sit"), TheAnswer(42)]`
            // I didn't even know c# supported this syntax.
            foreach(var attributeList in method.AttributeLists) {
                foreach(var attribute in attributeList.Attributes) {
                    // Note to self for future: https://stackoverflow.com/a/33966036
                    var attributeType = semantics.GetTypeInfo(attribute).ConvertedType;
                    // It shouts about this if I just ".Equals" the first with the second.
                    if (SymbolEqualityComparer.Default.Equals(attributeType, mcFunctionType)) {
                        mcFunctionMethods.Add(method);
                    }
                }
            }
        }
    }
}
