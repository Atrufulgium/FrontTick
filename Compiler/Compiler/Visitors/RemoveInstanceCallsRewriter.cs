using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Removes any code of the form
    /// <code>
    ///     public T1 Method(T2 a, .. ) { .. }
    /// </code>
    /// </para>
    /// </summary>
    public class RemoveInstanceCallsRewriter : AbstractFullRewriter {

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node) {
            if (!node.ChildTokensContain(SyntaxKind.StaticKeyword))
                return null;
            return base.VisitMethodDeclaration(node);
        }
    }
}
