using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// On <tt>if (true/false)</tt>, deletes the inaccessible branch and
    /// flattens the accesible branch.
    /// </summary>
    public class IfTrueFalseRewriter : AbstractFullRewriter<GuaranteeBlockRewriter> {

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node) {
            if (node.Condition is LiteralExpressionSyntax lit) {
                if (lit.Kind() == SyntaxKind.TrueLiteralExpression) {
                    return VisitBlock((BlockSyntax) node.Statement);
                } else if (lit.Kind() == SyntaxKind.FalseLiteralExpression) {
                    if (node.Else != null) {
                        return VisitBlock((BlockSyntax) node.Else.Statement);
                    } else {
                        return null;
                    }
                }
            }
            return base.VisitIfStatement(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            return ((BlockSyntax) base.VisitBlock(node)).Flattened();
        }
    }
}
