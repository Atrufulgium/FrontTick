using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Removes any method that is an operator declaration.
    /// </para>
    /// </summary>
    public class RemoveOperatorRewriter : AbstractFullRewriter {
        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node) => null;
    }
}
