using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Any method that has returns in it has the return value put in a temp
    /// var, and then <tt>goto</tt>s to a new label at the end of the method
    /// to return that temp var.
    /// </para>
    /// <para>
    /// Any code after a <tt>return</tt> at this stage is illegal and throws
    /// an exception.
    /// </para>
    /// </summary>
    public class ReturnRewriter : AbstractFullRewriter<GuaranteeBlockRewriter> {

        bool isVoid;

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node) {
            isVoid = node.ReturnsVoid();

            node = (MethodDeclarationSyntax) base.VisitMethodDeclaration(node);

            var newBody = node.Body;
            if (isVoid) {
                // Just returning
                newBody = newBody.WithAppendedStatement(
                    LabeledStatement(
                        NameManager.GetRetGotoName(),
                        ReturnStatement()
                    )
                );
            } else {
                // Init of the temp var
                newBody = newBody.WithPrependedStatement(
                    ExpressionStatement(
                        DeclarationExpression(
                            node.ReturnType,
                            SingleVariableDesignation(NameManager.GetRetName())
                        )
                    )
                );
                // Returning the temp var
                newBody = newBody.WithAppendedStatement(
                    LabeledStatement(
                        NameManager.GetRetGotoName(),
                        ReturnStatement(
                            IdentifierName(NameManager.GetRetName())
                        )
                    )
                );
            }

            return node.WithBody(newBody);
        }

        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node) {
            if (isVoid) {
                var ret = GotoStatement(NameManager.GetRetGotoName());
                return base.VisitGotoStatement(ret);
            } else {
                var ret = Block(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(NameManager.GetRetName()),
                            node.Expression
                        )
                    ),
                    GotoStatement(NameManager.GetRetGotoName())
                );
                return VisitBlock(ret);
            }
        }

        // We can introduce { { } } which we don't want.
        // Also, check whether or not there any statements after any returns,
        // because that hints at wrongly processed code in an earlier stage.
        public override SyntaxNode VisitBlock(BlockSyntax node) {
            bool foundReturn = false;
            foreach (var statement in node.Statements) {
                if (foundReturn)
                    throw CompilationException.ToDatapackReturnNoNonReturnAfterReturn;

                var checkStatement = statement;
                while (checkStatement is LabeledStatementSyntax labeled) {
                    checkStatement = labeled.Statement;
                }
                foundReturn |= checkStatement is ReturnStatementSyntax;
            }

            return ((BlockSyntax)base.VisitBlock(node)).Flattened();
        }
    }
}
