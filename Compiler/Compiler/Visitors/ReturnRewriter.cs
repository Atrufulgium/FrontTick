using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// <para>
    /// Any method that has returns in it has the return value put in #RET,
    /// and then <tt>goto</tt>s to a new label at the end of the method.
    /// </para>
    /// <para>
    /// Any code after a <tt>return</tt> at this stage is illegal and throws
    /// an exception.
    /// </para>
    /// </summary>
    /// <remarks>
    /// From here on out the "all stages output valid c#" invariant is broken.
    /// This is fine as there are not supposed to be many writes after this.
    /// </remarks>
    public class ReturnRewriter : AbstractFullRewriter<GuaranteeBlockRewriter> {

        bool isVoid;

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            if (node.IsExtern())
                return node;

            isVoid = node.ReturnsVoid();

            node = (MethodDeclarationSyntax) base.VisitMethodDeclarationRespectingNoCompile(node);

            var newBody = node.Body;
            if (!isVoid) {
                // Init of the return var
                newBody = newBody.WithPrependedStatement(
                    ExpressionStatement(
                        DeclarationExpression(
                            node.ReturnType,
                            SingleVariableDesignation(nameManager.GetRetName())
                        )
                    )
                );
            }
            var retStatement = isVoid ? ReturnStatement() : ReturnStatement(IdentifierName(nameManager.GetRetName()));
            var labeledRet = LabeledStatement(NameManager.GetRetGotoName(), Block(retStatement));
            // Due to GuaranteeBlockRewriter's "labels are followed by nothing"
            // guarantee, the end is of the form `label: .. label: statement;`
            // for some number ≥0 of labels.
            // We want to add this labled ret to the end of that.
            newBody = newBody.WithAppendedStatementThroughLabels(labeledRet);

            return node.WithBody(newBody);
        }

        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node) {
            if (isVoid) {
                var ret = GotoStatement(NameManager.GetRetGotoName());
                return base.VisitGotoStatement(ret);
            } else {
                // If we are the last return, doing a goto is incorrect.
                // This because this is the only place in the codebase that
                // creates a `goto A; B:` structure otherwise, which is
                // explicitely assumed not to be the case.
                StatementSyntax jumpStatement = GotoStatement(NameManager.GetRetGotoName());
                if (IsOriginallyRootScope(node))
                    jumpStatement = EmptyStatement();

                var ret = Block(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(nameManager.GetRetName()),
                            node.Expression
                        )
                    ),
                    jumpStatement
                );
                return VisitBlock(ret);
            }
        }

        // We can introduce { { } } which we don't want.
        // Also, check whether or not there any statements after any returns,
        // because that hints at wrongly processed code in an earlier stage.
        // Exception: The UnreachableCodeException.
        public override SyntaxNode VisitBlock(BlockSyntax node) {
            bool foundReturn = false;
            foreach (var statement in node.Statements) {
                if (foundReturn) {
                    if (statement is ThrowStatementSyntax th
                        && CurrentSemantics.TypesMatch(th.Expression, MCMirrorTypes.UnreachableCodeException))
                        continue;
                    throw CompilationException.ToDatapackReturnNoNonReturnAfterReturn;
                }

                var checkStatement = statement;
                while (checkStatement is LabeledStatementSyntax labeled) {
                    checkStatement = labeled.Statement;
                }
                foundReturn |= checkStatement is ReturnStatementSyntax;
            }

            return ((BlockSyntax)base.VisitBlock(node)).Flattened();
        }

        /// <summary>
        /// Returns whether going up to the root scope is achieved through only
        /// labeled blocks. This implies that this is the last return statement.
        /// </summary>
        public bool IsOriginallyRootScope(ReturnStatementSyntax ret) {
            BlockSyntax enclosingBlock = (BlockSyntax)ret.Parent;
            while (enclosingBlock.Parent is not MethodDeclarationSyntax) {
                if (enclosingBlock.Parent is not LabeledStatementSyntax label)
                    return false;
                if (label.Parent is not BlockSyntax block)
                    throw CompilationException.ToDatapackGotoLabelMustBeBlock;
                enclosingBlock = block;
            }
            return true;
        }
    }
}
