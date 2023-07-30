using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Atrufulgium.FrontTick.Compiler.SyntaxFactoryHelpers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atrufulgium.FrontTick.Compiler.Visitors {
    /// <summary>
    /// Turns any conditional
    /// <code>
    ///     if (complexExpression op literal) ..
    /// </code>
    /// into something like
    /// <code>
    ///     {
    ///         tempvar = complexExpression;
    ///         if (tempvar op literal) ..
    ///     }
    /// </code>
    /// </summary>
    /// <remarks>
    /// By relying on <see cref="LoopsToGotoCategory"/>, this also immediately
    /// applies this to all loop conditionals as well.
    /// </remarks>
    // TODO: Really, someday, implement bools and make this do the *obvious* transformation!
    // TODO II: Also, put this in an if-category with the other stuff in ProcessedToDatapack.
    public class SimplifyIfConditionRewriter : AbstractFullRewriter<LoopsToGotoCategory> {

        int tempCounter = 0;
        string GetTempName() => $"#IFTEMP{tempCounter++}";

        public override SyntaxNode VisitMethodDeclarationRespectingNoCompile(MethodDeclarationSyntax node) {
            tempCounter = 0;
            return base.VisitMethodDeclarationRespectingNoCompile(node);
        }

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node) {
            if (node.Condition is BinaryExpressionSyntax bin
                && bin.Left is not IdentifierNameSyntax) {
                
                var retType = CurrentSemantics.GetTypeInfo(bin.Left).Type;
                var name = GetTempName();

                return VisitBlock(
                    Block(
                        LocalDeclarationStatement(retType, name),
                        AssignmentStatement(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(name),
                            bin.Left
                        ),
                        node.WithCondition(
                            bin.WithLeft(
                                IdentifierName(name)
                            )
                        )
                    )
                );
            } else {
                return base.VisitIfStatement(node);
            }
        }

        public override SyntaxNode VisitBlock(BlockSyntax node) {
            return ((BlockSyntax) base.VisitBlock(node)).Flattened();
        }
    }
}
