using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// As we're working with a syntax tree, parentheses hold no meaning other
    /// than "annoy you with extra Syntax classes".
    /// </para>
    /// <para>
    /// This removes all <see cref="ParenthesizedExpressionSyntax"/>es.
    /// (This does not touch other parenthesized things yet.)
    /// </para>
    /// </summary>
    public class RemoveParenthesesRewriter : AbstractFullRewriter {

        public override SyntaxNode VisitParenthesizedExpression(ParenthesizedExpressionSyntax node) {
            return base.Visit(node.Expression);
        }
    }
}
