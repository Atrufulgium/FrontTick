using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Removes any constructor.
    /// </para>
    /// </summary>
    public class RemoveConstructorsRewriter : AbstractFullRewriter {
        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            => null;

        public override SyntaxNode VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
            => null;
    }
}
