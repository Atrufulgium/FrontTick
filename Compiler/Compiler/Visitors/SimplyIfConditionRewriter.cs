using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// Turns any conditional
    /// <code>
    ///     if (complexExpression) ..
    ///     [else .. ]
    /// </code>
    /// into something like
    /// <code>
    ///     {
    ///         bool tempvar = complexExpression;
    ///         if (tempvar) ..
    ///         [if (!tempvar) ..]
    ///     }
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// By relying on <see cref="LoopsToGotoCategory"/>, this also immediately
    /// applies this to all loop conditionals as well.
    /// </para>
    /// <para>
    /// Note that while this preserves semantics, c# doesn't like this rewrite
    /// when both branches return; it then sees a new path after the two
    /// branches. As such, introduce a
    /// <see cref="MCMirrorTypes.UnreachableCodeException"/>
    /// after every method.
    /// </para>
    /// </remarks>
    public class SimplifyIfConditionRewriter : AbstractFullRewriter<LoopsToGotoCategory> {
        int tempCounter = 0;
        string GetTempName() => $"#IFTEMP{tempCounter++}";

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            if (node.IsExtern())
                return node;

            tempCounter = 0;
            var methodDeclaration = (MethodDeclarationSyntax)base.VisitMethodDeclarationRespectingNoCompile(node);
            methodDeclaration = methodDeclaration.WithBody(
                methodDeclaration.Body.WithAppendedStatement(
                    ThrowStatement(MemberAccessExpression(MCMirrorTypes.UnreachableCodeException_Exception))
                )
            );
            return methodDeclaration;
        }

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node) {
            // Don't need to do anything when the condition is a boolean type
            // identifier name.
            var condition = node.Condition;
            // Positive `if (bleh)`
            if (condition is IdentifierNameSyntax id && CurrentSemantics.TypesMatch(id, MCMirrorTypes.Bool))
                return base.VisitIfStatement(node);
            // Negative `if (!bleh)`
            if (condition is PrefixUnaryExpressionSyntax un && un.IsKind(SyntaxKind.LogicalNotExpression)
                && un.Operand is IdentifierNameSyntax id2 && CurrentSemantics.TypesMatch(id2, MCMirrorTypes.Bool))
                return base.VisitIfStatement(node);
            
            // We're not simple and we need to extract.
            var block = Block();
            // (Note: do I need to handle casts?)
            var typeSymbol = CurrentSemantics.GetTypeInfo(condition).Type;
            var name = GetTempName();

            block = block.WithAppendedStatement(
                LocalDeclarationStatement(typeSymbol, name),
                AssignmentStatement(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(name),
                    condition
                ),
                IfStatement(
                    IdentifierName(name),
                    (StatementSyntax) base.Visit(node.Statement)
                )
            );

            if (node.Else != null) {
                block = block.WithAppendedStatement(
                    IfStatement(
                        PrefixUnaryExpression(
                            SyntaxKind.LogicalNotExpression,
                            IdentifierName(name)
                        ),
                        (StatementSyntax) base.Visit(node.Else.Statement)
                    )
                );
            }

            return block;
        }

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            return ((BlockSyntax) base.VisitBlock(node)).Flattened();
        }
    }
}
