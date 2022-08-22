using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// This is to guarantee that if/else/for/do/while/etc's statements are
    /// always blocks, instead of occasionally single statements when the
    /// original source doesn't use any <tt>{ }</tt>.
    /// </summary>
    public class GuaranteeBlockRewriter : AbstractFullRewriter {

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(SyntaxFactory.Block(node.Statement));
            }
            if (node.Else != null && node.Else.Statement is not BlockSyntax) {
                node = node.WithElse(SyntaxFactory.ElseClause(SyntaxFactory.Block(node.Else.Statement)));
            }
            return base.VisitIfStatement(node);
        }

        // Bit awkward how all of these are the literal same code, but they
        // don't share an interface.
        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(SyntaxFactory.Block(node.Statement));
            }
            return base.VisitWhileStatement(node);
        }

        public override SyntaxNode VisitDoStatement(DoStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(SyntaxFactory.Block(node.Statement));
            }
            return base.VisitDoStatement(node);
        }

        public override SyntaxNode VisitForStatement(ForStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(SyntaxFactory.Block(node.Statement));
            }
            return base.VisitForStatement(node);
        }

        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(SyntaxFactory.Block(node.Statement));
            }
            return base.VisitForEachStatement(node);
        }

        public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node) {
            if (node.Statement is not BlockSyntax) {
                node = node.WithStatement(SyntaxFactory.Block(node.Statement));
            }
            return base.VisitUsingStatement(node);
        }
    }
}
