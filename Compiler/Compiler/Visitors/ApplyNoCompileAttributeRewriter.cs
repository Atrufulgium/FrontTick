using MCMirror.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// A walker that removes all [NoCompile]-attributed code from compilation.
    /// </summary>
    public class ApplyNoCompileAttributeRewriter : AbstractFullRewriter {

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node) {
            if (CurrentSemantics.TryGetAttributeOfType(node, typeof(NoCompileAttribute), out _))
                return null;
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node) {
            if (CurrentSemantics.TryGetAttributeOfType(node, typeof(NoCompileAttribute), out _)) {
                return null;
            }
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node) {
            if (CurrentSemantics.TryGetAttributeOfType(node, typeof(NoCompileAttribute), out _))
                return null;
            return base.VisitStructDeclaration(node);
        }
    }
}
